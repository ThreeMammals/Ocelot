using KubeClient;
using KubeClient.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Logging;
using Ocelot.Provider.Kubernetes;
using Ocelot.Provider.Kubernetes.Interfaces;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;
using Shouldly;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;

namespace Ocelot.AcceptanceTests.ServiceDiscovery;

public sealed class KubernetesServiceDiscoveryTests : Steps, IDisposable
{
    private readonly string _kubernetesUrl;
    private readonly IKubeApiClient _clientFactory;
    private readonly List<ServiceHandler> _serviceHandlers;
    private readonly ServiceHandler _kubernetesHandler;
    private string _receivedToken;

    public KubernetesServiceDiscoveryTests()
    {
        _kubernetesUrl = DownstreamUrl(PortFinder.GetRandomPort());
        var option = new KubeClientOptions
        {
            ApiEndPoint = new Uri(_kubernetesUrl),
            AccessToken = "txpc696iUhbVoudg164r93CxDTrKRVWG",
            AuthStrategy = KubeAuthStrategy.BearerToken,
            AllowInsecure = true,
        };
        _clientFactory = KubeApiClient.Create(option);
        _serviceHandlers = new();
        _kubernetesHandler = new();
    }

    public override void Dispose()
    {
        _serviceHandlers.ForEach(handler => handler?.Dispose());
        _kubernetesHandler.Dispose();
        base.Dispose();
    }

    [Fact]
    public void ShouldReturnServicesFromK8s()
    {
        const string namespaces = nameof(KubernetesServiceDiscoveryTests);
        const string serviceName = nameof(ShouldReturnServicesFromK8s);
        var servicePort = PortFinder.GetRandomPort();
        var downstreamUrl = LoopbackLocalhostUrl(servicePort);
        var downstream = new Uri(downstreamUrl);
        var subsetV1 = GivenSubsetAddress(downstream);
        var endpoints = GivenEndpoints(subsetV1);
        var route = GivenRouteWithServiceName(namespaces);
        var configuration = GivenKubeConfiguration(namespaces, route);
        var downstreamResponse = serviceName;
        this.Given(x => x.GivenK8sProductServiceIsRunning(downstreamUrl, downstreamResponse))
            .And(x => x.GivenThereIsAFakeKubernetesProvider(serviceName, namespaces, endpoints))
            .And(_ => GivenThereIsAConfiguration(configuration))
            .And(_ => GivenOcelotIsRunningWithServices(WithKubernetes))
            .When(_ => WhenIGetUrlOnTheApiGateway("/"))
            .Then(_ => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(_ => ThenTheResponseBodyShouldBe($"1:{downstreamResponse}"))
            .And(x => x.ThenTheTokenIs("Bearer txpc696iUhbVoudg164r93CxDTrKRVWG"))
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
        var downstreamUrl = LoopbackLocalhostUrl(servicePort);
        var downstream = new Uri(downstreamUrl);
        var subsetV1 = GivenSubsetAddress(downstream);

        // Ports[0] -> port(https, 443)
        // Ports[1] -> port(http, not 80)
        subsetV1.Ports.Insert(0, new()
        {
            Name = "https", // This service instance is offline -> BadGateway
            Port = 443,
        });
        var endpoints = GivenEndpoints(subsetV1);
        var route = GivenRouteWithServiceName(namespaces);
        route.DownstreamPathTemplate = "/{url}";
        route.DownstreamScheme = downstreamScheme; // !!! Warning !!! Select port by name as scheme
        route.UpstreamPathTemplate = "/api/example/{url}";
        route.ServiceName = serviceName; // "example-web"
        var configuration = GivenKubeConfiguration(namespaces, route);

        this.Given(x => x.GivenK8sProductServiceIsRunning(downstreamUrl, nameof(ShouldReturnServicesByPortNameAsDownstreamScheme)))
            .And(x => x.GivenThereIsAFakeKubernetesProvider(serviceName, namespaces, endpoints))
            .And(_ => GivenThereIsAConfiguration(configuration))
            .And(_ => GivenOcelotIsRunningWithServices(WithKubernetes))
            .When(_ => WhenIGetUrlOnTheApiGateway("/api/example/1"))
            .Then(_ => ThenTheStatusCodeShouldBe(statusCode))
            .And(_ => ThenTheResponseBodyShouldBe(downstreamScheme == "http"
                    ? "1:" + nameof(ShouldReturnServicesByPortNameAsDownstreamScheme)
                    : string.Empty))
            .And(x => x.ThenTheTokenIs("Bearer txpc696iUhbVoudg164r93CxDTrKRVWG"))
            .BDDfy();
    }

    [Theory]
    [Trait("Bug", "2110")]
    [InlineData(true)]
    [InlineData(false)]
    public void ShouldReturnServicesFromK8s_HavingHighLoadOnTheProviderAndRoundRobinBalancer(bool isK8sIntegrationStable)
    {
        // Arrange
        const int totalServices = 5, totalRequests = 50;
        const string namespaces = nameof(KubernetesServiceDiscoveryTests);
        const string serviceName = nameof(ShouldReturnServicesFromK8s_HavingHighLoadOnTheProviderAndRoundRobinBalancer);
        var servicePorts = Enumerable.Repeat(0, totalServices)
            .Select(_ => PortFinder.GetRandomPort())
            .ToArray();
        var downstreamUrls = servicePorts
            .Select(port => LoopbackLocalhostUrl(port, Array.IndexOf(servicePorts, port)))
            .ToList(); // based on localhost aka loopback network interface
        var downstreams = downstreamUrls
            .Select(url => new Uri(url))
            .ToList();
        var downstreamResponses = downstreams
            .Select(ds => $"{serviceName}:{ds.Host}:{ds.Port}")
            .ToList();
        var subset = new EndpointSubsetV1();
        downstreams.ForEach(ds => GivenSubsetAddress(ds, subset));
        var endpoints = GivenEndpoints(subset); // total 3 service instances with different ports
        var route = GivenRouteWithServiceName(namespaces, loadBalancerType: nameof(RoundRobin)); // !!!
        var configuration = GivenKubeConfiguration(namespaces, route);
        int bottom = totalRequests / totalServices,
            top = totalRequests - (bottom * totalServices) + bottom;
        GivenMultipleK8sProductServicesAreRunning(downstreamUrls, downstreamResponses, totalRequests);
        GivenThereIsAFakeKubernetesProvider(serviceName, namespaces, endpoints, isK8sIntegrationStable); // with stability option
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunningWithServices(WithKubernetesAndRoundRobin);

        // Act
        WhenIGetUrlOnTheApiGatewayMultipleTimes("/", totalRequests); // load by 50 parallel requests

        // Assert
        ThenAllStatusCodesShouldBe(HttpStatusCode.OK);
        ThenAllServicesShouldHaveBeenCalledTimes(totalRequests);
        ThenAllServicesCalledRealisticAmountOfTimes(bottom, top);
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

    private static EndpointSubsetV1 GivenSubsetAddress(Uri downstream, EndpointSubsetV1 subset = null)
    {
        subset ??= new();
        subset.Addresses.Add(new()
        {
            Ip = Dns.GetHostAddresses(downstream.Host).Select(x => x.ToString()).First(a => a.Contains('.')), // 127.0.0.1
            Hostname = downstream.Host,
        });
        subset.Ports.Add(new()
        {
            Name = downstream.Scheme,
            Port = downstream.Port,
        });
        return subset;
    }

    private FileRoute GivenRouteWithServiceName(string serviceNamespace,
        [CallerMemberName] string serviceName = null,
        string loadBalancerType = nameof(LeastConnection))
        => new()
        {
            DownstreamPathTemplate = "/",
            DownstreamScheme = null, // the scheme should not be defined in service discovery scenarios by default, only ServiceName
            UpstreamPathTemplate = "/",
            UpstreamHttpMethod = new() { HttpMethods.Get },
            ServiceName = serviceName, // !!!
            ServiceNamespace = serviceNamespace,
            LoadBalancerOptions = new() { Type = loadBalancerType },
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
        => GivenThereIsAFakeKubernetesProvider(serviceName, namespaces, endpoints, true);

    private void GivenThereIsAFakeKubernetesProvider(string serviceName, string namespaces, EndpointsV1 endpoints, bool isStable)
    {
        _k8sCounter = 0;
        _kubernetesHandler.GivenThereIsAServiceRunningOn(_kubernetesUrl, async context =>
        {
            if (context.Request.Path.Value == $"/api/v1/namespaces/{namespaces}/endpoints/{serviceName}")
            {
                // Each 20th request to integrated K8s endpoint should fail
                //lock (_k8sCounterSyncRoot)
                //{
                _k8sCounter++;
                if (!isStable && _k8sCounter % 20 == 0 && _k8sCounter >= 20)
                {
                    var subset = endpoints.Subsets[0];
                    var onlineAddress = subset.Addresses[0];
                    subset.Addresses.Clear(); // all services go offline
                    subset.Addresses.Add(onlineAddress); // make online the 1st instance only
                    var onlinePort = subset.Ports[0];
                    subset.Ports.Clear();
                    subset.Ports.Add(onlinePort);
                    _k8sTotalFailed++;
                }
                //}

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

    private void WithKubernetes(IServiceCollection services) => services
        .AddOcelot().AddKubernetes()
        .Services.RemoveAll<IKubeApiClient>().AddSingleton(_clientFactory);

    private void WithKubernetesAndRoundRobin(IServiceCollection services) => services
        .AddOcelot().AddKubernetes()
        //.AddCustomLoadBalancer<RoundRobinCounter>((_, _, provider) => _roundRobinCounter = new RoundRobinCounter(provider.GetAsync))
        .Services
        .RemoveAll<IKubeApiClient>().AddSingleton(_clientFactory)
        .RemoveAll<IKubeServiceCreator>().AddSingleton<IKubeServiceCreator, FakeKubeServiceCreator>();

    private ConcurrentDictionary<int, int> _productServiceCounters;
    private RoundRobinCounter _roundRobinCounter;
    private int _k8sCounter;
    private int _k8sTotalFailed;
    //private static object _k8sCounterSyncRoot = new();

    private void GivenK8sProductServiceIsRunning(string url, string response)
    {
        _serviceHandlers.Add(new()); // allocate single instance
        _productServiceCounters = new(2, 1); // single counter
        GivenK8sProductServiceIsRunning(url, response, 0);
        _productServiceCounters[0] = 0;
    }

    private void GivenMultipleK8sProductServicesAreRunning(List<string> urls, List<string> responses, int totalThreads)
    {
        urls.ForEach(_ => _serviceHandlers.Add(new())); // allocate multiple instances
        _productServiceCounters = new(totalThreads, urls.Count); // multiple counters
        for (int i = 0; i < urls.Count; i++)
        {
            GivenK8sProductServiceIsRunning(urls[i], responses[i], i);
            _productServiceCounters[i] = 0;
        }
    }

    private void GivenK8sProductServiceIsRunning(string url, string response, int handlerIndex)
    {
        var serviceHandler = _serviceHandlers[handlerIndex];
        serviceHandler.GivenThereIsAServiceRunningOn(url,
            async context =>
            {
                try
                {
                    _productServiceCounters[handlerIndex]++;
                    var threadResponse = string.Concat(_productServiceCounters[handlerIndex], ':', response);

                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    await context.Response.WriteAsync(threadResponse ?? ((int)HttpStatusCode.OK).ToString());
                }
                catch (Exception exception)
                {
                    await context.Response.WriteAsync(exception.StackTrace);
                }
            });
    }

    private void ThenAllServicesShouldHaveBeenCalledTimes(int expected)
    {
        var customMessage = $"All values are [{string.Join(',', _productServiceCounters.Values)}]";
        _productServiceCounters.Values.Sum().ShouldBe(expected, customMessage);
        //_roundRobinCounter.Counters.Values.Sum().ShouldBe(expected);
    }

    private void ThenAllServicesCalledRealisticAmountOfTimes(int bottom, int top)
    {
        var customMessage = $"{nameof(bottom)}: {bottom}\n\t{nameof(top)}: {top}\n\tAll values are [{string.Join(',', _productServiceCounters.Values)}]";
        _productServiceCounters.Values.ShouldAllBe(counter => bottom <= counter && counter <= top, customMessage);
        //_roundRobinCounter.Counters.Values.ShouldAllBe(c => shouldInRange(c));
    }
}

internal class FakeKubeServiceCreator : KubeServiceCreator
{
    public FakeKubeServiceCreator(IOcelotLoggerFactory factory) : base(factory) { }

    protected override ServiceHostAndPort GetServiceHostAndPort(KubeRegistryConfiguration configuration, EndpointsV1 endpoint, EndpointSubsetV1 subset, EndpointAddressV1 address)
    {
        //return base.GetServiceHostAndPort(configuration, endpoint, subset, address);
        var ports = subset.Ports;
        var index = subset.Addresses.IndexOf(address);
        var portV1 = ports[index];
        Logger.LogDebug(() => $"K8s service with key '{configuration.KeyOfServiceInK8s}' and address {address.Ip}; Detected port is {portV1.Name}:{portV1.Port}. Total {ports.Count} ports of [{string.Join(',', ports.Select(p => p.Name))}].");
        return new ServiceHostAndPort(address.Ip, portV1.Port, portV1.Name);
    }
}

internal class RoundRobinCounter : RoundRobin, ILoadBalancer
{
    public readonly Dictionary<int, int> Counters = new();

    public RoundRobinCounter(Func<Task<List<Service>>> services) : base(services) { }

    public async override Task<Response<ServiceHostAndPort>> Lease(HttpContext httpContext)
    {
        var response = await base.Lease(httpContext);
        if (response?.IsError == true)
        {
            return response;
        }

        lock (SyncRoot)
        {
            int key = Last - 1; // real processed index
            if (!Counters.TryGetValue(key, out int value))
            {
                value = 0;
            }

            Counters[key] = ++value;
        }

        return response;
    }
}
