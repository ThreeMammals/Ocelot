using KubeClient;
using KubeClient.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Provider.Kubernetes;
using System.Runtime.CompilerServices;

namespace Ocelot.AcceptanceTests.ServiceDiscovery;

public sealed class KubernetesServiceDiscoveryTests : Steps, IDisposable
{
    private readonly string _kubernetesUrl;
    private readonly IKubeApiClient _clientFactory;
    private readonly ServiceHandler _serviceHandler;
    private readonly ServiceHandler _kubernetesHandler;
    private string _receivedToken;

    public KubernetesServiceDiscoveryTests()
    {
        _kubernetesUrl = DownstreamUrl(PortFinder.GetRandomPort()); //5567
        var option = new KubeClientOptions
        {
            ApiEndPoint = new Uri(_kubernetesUrl),
            AccessToken = "txpc696iUhbVoudg164r93CxDTrKRVWG",
            AuthStrategy = KubeAuthStrategy.BearerToken,
            AllowInsecure = true,
        };
        _clientFactory = KubeApiClient.Create(option);
        _serviceHandler = new ServiceHandler();
        _kubernetesHandler = new ServiceHandler();
    }

    public override void Dispose()
    {
        _serviceHandler.Dispose();
        _kubernetesHandler.Dispose();
        base.Dispose();
    }

    [Fact]
    public void ShouldReturnServicesFromK8s()
    {
        const string namespaces = nameof(KubernetesServiceDiscoveryTests);
        const string serviceName = nameof(ShouldReturnServicesFromK8s);
        var servicePort = PortFinder.GetRandomPort();
        var downstreamUrl = DownstreamUrl(servicePort);
        var downstream = new Uri(downstreamUrl);
        var subsetV1 = new EndpointSubsetV1();
        subsetV1.Addresses.Add(new()
        {
            Ip = Dns.GetHostAddresses(downstream.Host).Select(x => x.ToString()).First(a => a.Contains('.')),
            Hostname = downstream.Host,
        });
        subsetV1.Ports.Add(new()
        {
            Name = downstream.Scheme,
            Port = servicePort,
        });
        var endpoints = GivenEndpoints(subsetV1);
        var route = GivenRouteWithServiceName(namespaces);
        var configuration = GivenKubeConfiguration(namespaces, route);
        var downstreamResponse = serviceName;
        this.Given(x => GivenK8sProductServiceOneIsRunning(downstreamUrl, downstreamResponse))
            .And(x => GivenThereIsAFakeKubernetesProvider(serviceName, namespaces, endpoints))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => x.GivenOcelotIsRunningWithKubernetes())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(_ => ThenTheResponseBodyShouldBe(downstreamResponse))
            .And(_ => ThenTheTokenIs("Bearer txpc696iUhbVoudg164r93CxDTrKRVWG"))
            .BDDfy();
    }

    [Theory]
    [Trait("Feat", "1967")]
    [InlineData("", HttpStatusCode.BadGateway)]
    [InlineData("http", HttpStatusCode.OK)]
    public void ShouldReturnServicesByPortNameAsDownstreamScheme(string downstreamScheme, HttpStatusCode statusCode)
    {
        const string serviceName = "example-web";
        const string namespaces = "default";
        var servicePort = PortFinder.GetRandomPort();
        var downstreamUrl = DownstreamUrl(servicePort);
        var downstream = new Uri(downstreamUrl);

        var subsetV1 = new EndpointSubsetV1();
        subsetV1.Addresses.Add(new()
        {
            Ip = Dns.GetHostAddresses(downstream.Host).Select(x => x.ToString()).First(a => a.Contains('.')),
            Hostname = downstream.Host,
        });
        subsetV1.Ports.Add(new()
        {
            Name = "https", // This service instance is offline -> BadGateway
            Port = 443,
        });
        subsetV1.Ports.Add(new()
        {
            Name = downstream.Scheme, // http, should be real scheme
            Port = downstream.Port, // not 80, should be real port
        });
        var endpoints = GivenEndpoints(subsetV1);

        var route = GivenRouteWithServiceName(namespaces);
        route.DownstreamPathTemplate = "/{url}";
        route.DownstreamScheme = downstreamScheme; // !!! Warning !!! Select port by name as scheme
        route.UpstreamPathTemplate = "/api/example/{url}";
        route.ServiceName = serviceName; // "example-web"
        var configuration = GivenKubeConfiguration(namespaces, route);

        this.Given(x => GivenK8sProductServiceOneIsRunning(downstreamUrl, nameof(ShouldReturnServicesByPortNameAsDownstreamScheme)))
            .And(x => GivenThereIsAFakeKubernetesProvider(serviceName, namespaces, endpoints))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => x.GivenOcelotIsRunningWithKubernetes())
            .When(x => WhenIGetUrlOnTheApiGateway("/api/example/1"))
            .Then(x => ThenTheStatusCodeShouldBe(statusCode))
            .And(_ => ThenTheResponseBodyShouldBe(downstreamScheme == "http"
                    ? nameof(ShouldReturnServicesByPortNameAsDownstreamScheme) : string.Empty))
            .And(_ => ThenTheTokenIs("Bearer txpc696iUhbVoudg164r93CxDTrKRVWG"))
            .BDDfy();
    }

    private void ThenTheTokenIs(string token)
    {
        _receivedToken.ShouldBe(token);
    }

    private EndpointsV1 GivenEndpoints(EndpointSubsetV1 subset, [CallerMemberName] string serviceName = "")
    {
        var e = new EndpointsV1()
        {
            Kind = "endpoint",
            ApiVersion = "1.0",
            Metadata = new()
            {
                Name = serviceName,
                Namespace = nameof(KubernetesServiceDiscoveryTests),
            },
        };
        e.Subsets.Add(subset);
        return e;
    }

    private FileRoute GivenRouteWithServiceName(string serviceNamespace, [CallerMemberName] string serviceName = null) => new()
    {
        DownstreamPathTemplate = "/",
        DownstreamScheme = Uri.UriSchemeHttp,
        UpstreamPathTemplate = "/",
        UpstreamHttpMethod = new() { HttpMethods.Get },
        ServiceName = serviceName,
        ServiceNamespace = serviceNamespace,
        LoadBalancerOptions = new() { Type = nameof(LeastConnection) },
    };

    private FileConfiguration GivenKubeConfiguration(string serviceNamespace, params FileRoute[] routes)
    {
        var u = new Uri(_kubernetesUrl);
        var configuration = GivenConfiguration(routes);
        configuration.GlobalConfiguration.ServiceDiscoveryProvider = new()
        {
            Scheme = u.Scheme,
            Host = u.Host,
            Port = u.Port,
            Type = nameof(Kube),
            PollingInterval = 0,
            Namespace = serviceNamespace,
        };
        return configuration;
    }

    private void GivenThereIsAFakeKubernetesProvider(string serviceName, string namespaces, EndpointsV1 endpoints)
        => _kubernetesHandler.GivenThereIsAServiceRunningOn(_kubernetesUrl, async context =>
        {
            if (context.Request.Path.Value == $"/api/v1/namespaces/{namespaces}/endpoints/{serviceName}")
            {
                if (context.Request.Headers.TryGetValue("Authorization", out var values))
                {
                    _receivedToken = values.First();
                }

                var json = JsonConvert.SerializeObject(endpoints);
                context.Response.Headers.Append("Content-Type", "application/json");
                await context.Response.WriteAsync(json);
            }
        });

    private void GivenOcelotIsRunningWithKubernetes()
        => GivenOcelotIsRunningWithServices(s =>
        {
            s.AddOcelot().AddKubernetes();
            s.RemoveAll<IKubeApiClient>().AddSingleton(_clientFactory);
        });

    private void GivenK8sProductServiceOneIsRunning(string url, string response)
        => _serviceHandler.GivenThereIsAServiceRunningOn(url, async context =>
        {
            try
            {
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                await context.Response.WriteAsync(response ?? nameof(HttpStatusCode.OK));
            }
            catch (Exception exception)
            {
                await context.Response.WriteAsync(exception.StackTrace);
            }
        });
}
