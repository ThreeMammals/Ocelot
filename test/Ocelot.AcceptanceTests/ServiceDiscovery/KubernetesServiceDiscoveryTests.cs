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
    private string _receivedToken;
    private readonly IKubeApiClient _clientFactory;
    private readonly ServiceHandler _serviceHandler;
    private readonly ServiceHandler _kubernetesHandler;

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
    public void Should_return_services_from_K8s()
    {
        const string namespaces = nameof(KubernetesServiceDiscoveryTests);
        const string serviceName = nameof(Should_return_services_from_K8s);
        var token = "Bearer txpc696iUhbVoudg164r93CxDTrKRVWG";
        var endpoints = new EndpointsV1
        {
            Kind = "endpoint",
            ApiVersion = "1.0",
            Metadata = new()
            {
                Name = serviceName,
                Namespace = namespaces,
            },
        };
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
        endpoints.Subsets.Add(subsetV1);
        var route = GivenRouteWithServiceName(namespaces);
        var configuration = GivenKubeConfiguration(namespaces, route);
        var downstreamResponse = serviceName;
        this.Given(x => GivenK8sProductServiceOneIsRunning(downstreamUrl, downstreamResponse))
            .And(x => GivenThereIsAFakeKubernetesProvider(_kubernetesUrl, serviceName, namespaces, endpoints))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => x.GivenOcelotIsRunningWithKubernetes())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(_ => ThenTheResponseBodyShouldBe(downstreamResponse))
            .And(_ => ThenTheTokenIs(token))
            .BDDfy();
    }

    private void ThenTheTokenIs(string token)
    {
        _receivedToken.ShouldBe(token);
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

    private void GivenThereIsAFakeKubernetesProvider(string url, string serviceName, string namespaces, EndpointsV1 endpoints)
    {
        _kubernetesHandler.GivenThereIsAServiceRunningOn(url, async context =>
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
    }

    private void GivenOcelotIsRunningWithKubernetes()
        => GivenOcelotIsRunningWithServices(s =>
        {
            s.AddOcelot().AddKubernetes();
            s.RemoveAll<IKubeApiClient>().AddSingleton(_clientFactory);
        });

    private void GivenK8sProductServiceOneIsRunning(string url, string response)
    {
        _serviceHandler.GivenThereIsAServiceRunningOn(url, async context =>
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
}
