using KubeClient;
using KubeClient.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;
using Ocelot.AcceptanceTests.LoadBalancer;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Logging;
using Ocelot.Provider.Kubernetes;
using Ocelot.Provider.Kubernetes.Interfaces;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;
using System.Runtime.CompilerServices;

namespace Ocelot.AcceptanceTests.ServiceDiscovery;

public sealed class KubernetesServiceDiscoveryTests : ConcurrentSteps, IDisposable
{
    private readonly string _kubernetesUrl;
    private readonly ServiceHandler _kubernetesHandler;
    private string _receivedToken;
    private readonly Action<KubeClientOptions> _kubeClientOptionsConfigure;

    public KubernetesServiceDiscoveryTests()
    {
        _kubernetesUrl = DownstreamUrl(PortFinder.GetRandomPort());
        _kubeClientOptionsConfigure = opts =>
        {
            opts.ApiEndPoint = new Uri(_kubernetesUrl);
            opts.AccessToken = "txpc696iUhbVoudg164r93CxDTrKRVWG";
            opts.AuthStrategy = KubeAuthStrategy.BearerToken;
            opts.AllowInsecure = true;
        };
        _kubernetesHandler = new();
    }

    public override void Dispose()
    {
        _kubernetesHandler.Dispose();
        base.Dispose();
    }

    [Theory]
    [InlineData(nameof(Kube))]
    [InlineData(nameof(WatchKube))]
    public void ShouldReturnServicesFromK8s(string discoveryType)
    {
        const string namespaces = nameof(KubernetesServiceDiscoveryTests);
        const string serviceName = nameof(ShouldReturnServicesFromK8s);
        var servicePort = PortFinder.GetRandomPort();
        var downstreamUrl = LoopbackLocalhostUrl(servicePort);
        var downstream = new Uri(downstreamUrl);
        var subsetV1 = GivenSubsetAddress(downstream);
        var endpoints = GivenEndpoints(subsetV1);
        var route = GivenRouteWithServiceName(namespaces);
        var configuration = GivenKubeConfiguration(namespaces, discoveryType, route);
        var downstreamResponse = serviceName;
        this.Given(x => GivenServiceInstanceIsRunning(downstreamUrl, downstreamResponse))
            .And(x => x.GivenThereIsAFakeKubernetesProvider(endpoints, serviceName, namespaces))
            .And(_ => GivenThereIsAConfiguration(configuration))
            .And(_ => GivenOcelotIsRunningWithServices(WithKubernetes, true))
            .When(_ => WhenIGetUrlOnTheApiGateway("/"))
            .Then(_ => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(_ => ThenTheResponseBodyShouldBe($"1:{downstreamResponse}"))
            .And(x => ThenAllServicesShouldHaveBeenCalledTimes(1))
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
        var configuration = GivenKubeConfiguration(namespaces, nameof(Kube), route);

        this.Given(x => GivenServiceInstanceIsRunning(downstreamUrl, nameof(ShouldReturnServicesByPortNameAsDownstreamScheme)))
            .And(x => x.GivenThereIsAFakeKubernetesProvider(endpoints, serviceName, namespaces))
            .And(_ => GivenThereIsAConfiguration(configuration))
            .And(_ => GivenOcelotIsRunningWithServices(WithKubernetes, true))
            .When(_ => WhenIGetUrlOnTheApiGateway("/api/example/1"))
            .Then(_ => ThenTheStatusCodeShouldBe(statusCode))
            .And(_ => ThenTheResponseBodyShouldBe(downstreamScheme == "http"
                    ? "1:" + nameof(ShouldReturnServicesByPortNameAsDownstreamScheme)
                    : string.Empty))
            .And(x => ThenAllServicesShouldHaveBeenCalledTimes(downstreamScheme == "http" ? 1 : 0))
            .And(x => x.ThenTheTokenIs("Bearer txpc696iUhbVoudg164r93CxDTrKRVWG"))
            .BDDfy();
    }

    [Theory]
    [Trait("Bug", "2110")]
    [InlineData(1, 30)]
    [InlineData(2, 50)]
    [InlineData(3, 50)]
    [InlineData(4, 50)]
    [InlineData(5, 50)]
    [InlineData(6, 99)]
    [InlineData(7, 99)]
    [InlineData(8, 99)]
    [InlineData(9, 999)]
    [InlineData(10, 999)]
    public void ShouldHighlyLoadOnStableKubeProvider_WithRoundRobinLoadBalancing(int totalServices, int totalRequests)
    {
        const int ZeroGeneration = 0;
        var (endpoints, servicePorts) = ArrangeHighLoadOnKubeProviderAndRoundRobinBalancer(totalServices);
        GivenThereIsAFakeKubernetesProvider(endpoints); // stable, services will not be removed from the list

        HighlyLoadOnKubeProviderAndRoundRobinBalancer(totalRequests, ZeroGeneration);

        int bottom = totalRequests / totalServices,
            top = totalRequests - (bottom * totalServices) + bottom;
        ThenAllServicesCalledRealisticAmountOfTimes(bottom, top);
        ThenServiceCountersShouldMatchLeasingCounters(_roundRobinAnalyzer, servicePorts, totalRequests);
    }

    [Theory]
    [Trait("Bug", "2110")]
    [InlineData(5, 50, 1)]
    [InlineData(5, 50, 2)]
    [InlineData(5, 50, 3)]
    [InlineData(5, 50, 4)]
    public void ShouldHighlyLoadOnUnstableKubeProvider_WithRoundRobinLoadBalancing(int totalServices, int totalRequests, int k8sGeneration)
    {
        int failPerThreads = (totalRequests / k8sGeneration) - 1; // k8sGeneration means number of offline services
        var (endpoints, servicePorts) = ArrangeHighLoadOnKubeProviderAndRoundRobinBalancer(totalServices);
        GivenThereIsAFakeKubernetesProvider(endpoints, false, k8sGeneration, failPerThreads); // false means unstable, k8sGeneration services will be removed from the list

        HighlyLoadOnKubeProviderAndRoundRobinBalancer(totalRequests, k8sGeneration);

        ThenAllServicesCalledOptimisticAmountOfTimes(_roundRobinAnalyzer); // with unstable checkings
        ThenServiceCountersShouldMatchLeasingCounters(_roundRobinAnalyzer, servicePorts, totalRequests);
    }

    private (EndpointsV1 Endpoints, int[] ServicePorts) ArrangeHighLoadOnKubeProviderAndRoundRobinBalancer(
        int totalServices,
        [CallerMemberName] string serviceName = nameof(ArrangeHighLoadOnKubeProviderAndRoundRobinBalancer))
    {
        const string namespaces = nameof(KubernetesServiceDiscoveryTests);
        var servicePorts = PortFinder.GetPorts(totalServices);
        var downstreamUrls = servicePorts
            .Select(port => LoopbackLocalhostUrl(port, Array.IndexOf(servicePorts, port)))
            .ToArray(); // based on localhost aka loopback network interface
        var downstreams = downstreamUrls.Select(url => new Uri(url))
            .ToList();
        var downstreamResponses = downstreams
            .Select(ds => $"{serviceName}:{ds.Host}:{ds.Port}")
            .ToArray();
        var subset = new EndpointSubsetV1();
        downstreams.ForEach(ds => GivenSubsetAddress(ds, subset));
        var endpoints = GivenEndpoints(subset, serviceName); // totalServices service instances with different ports
        var route = GivenRouteWithServiceName(namespaces, serviceName, nameof(RoundRobinAnalyzer)); // !!!
        var configuration = GivenKubeConfiguration(namespaces, nameof(Kube), route);
        GivenMultipleServiceInstancesAreRunning(downstreamUrls, downstreamResponses);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunningWithServices(WithKubernetesAndRoundRobin, true);
        return (endpoints, servicePorts);
    }

    private void HighlyLoadOnKubeProviderAndRoundRobinBalancer(int totalRequests, int k8sGenerationNo)
    {
        // Act
        WhenIGetUrlOnTheApiGatewayConcurrently("/", totalRequests); // load by X parallel requests

        // Assert
        _k8sCounter.ShouldBeGreaterThanOrEqualTo(totalRequests); // integration endpoint called times
        _k8sServiceGeneration.ShouldBe(k8sGenerationNo);
        ThenAllStatusCodesShouldBe(HttpStatusCode.OK);
        ThenAllServicesShouldHaveBeenCalledTimes(totalRequests);
        _roundRobinAnalyzer.ShouldNotBeNull().Analyze();
        _roundRobinAnalyzer.Events.Count.ShouldBe(totalRequests);
        _roundRobinAnalyzer.HasManyServiceGenerations(k8sGenerationNo).ShouldBeTrue();
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
        string loadBalancerType = nameof(LeastConnection)) => new()
        {
            DownstreamPathTemplate = "/",
            DownstreamScheme = null, // the scheme should not be defined in service discovery scenarios by default, only ServiceName
            UpstreamPathTemplate = "/",
            UpstreamHttpMethod = new() { HttpMethods.Get },
            ServiceName = serviceName, // !!!
            ServiceNamespace = serviceNamespace,
            LoadBalancerOptions = new() { Type = loadBalancerType },
        };

    private FileConfiguration GivenKubeConfiguration(string serviceNamespace, string type, params FileRoute[] routes)
    {
        var u = new Uri(_kubernetesUrl);
        var configuration = GivenConfiguration(routes);
        configuration.GlobalConfiguration.ServiceDiscoveryProvider = new()
        {
            Scheme = u.Scheme,
            Host = u.Host,
            Port = u.Port,
            Type = type,
            PollingInterval = 0,
            Namespace = serviceNamespace,
        };
        return configuration;
    }

    private void GivenThereIsAFakeKubernetesProvider(EndpointsV1 endpoints,
        [CallerMemberName] string serviceName = nameof(KubernetesServiceDiscoveryTests), string namespaces = nameof(KubernetesServiceDiscoveryTests))
        => GivenThereIsAFakeKubernetesProvider(endpoints, true, 0, 0, serviceName, namespaces);

    private void GivenThereIsAFakeKubernetesProvider(EndpointsV1 endpoints, bool isStable, int offlineServicesNo, int offlinePerThreads,
        [CallerMemberName] string serviceName = nameof(KubernetesServiceDiscoveryTests), string namespaces = nameof(KubernetesServiceDiscoveryTests))
    {
        _k8sCounter = 0;
        _kubernetesHandler.GivenThereIsAServiceRunningOn(_kubernetesUrl, async context =>
        {
            await Task.Delay(Random.Shared.Next(1, 10)); // emulate integration delay up to 10 milliseconds
            if (context.Request.Path.Value == $"/api/v1/namespaces/{namespaces}/endpoints/{serviceName}")
            {
                string json;
                lock (K8sCounterLocker)
                {
                    _k8sCounter++;
                    var subset = endpoints.Subsets[0];

                    // Each offlinePerThreads-th request to integrated K8s endpoint should fail
                    if (!isStable && _k8sCounter % offlinePerThreads == 0 && _k8sCounter >= offlinePerThreads)
                    {
                        while (offlineServicesNo-- > 0)
                        {
                            int index = subset.Addresses.Count - 1; // Random.Shared.Next(0, subset.Addresses.Count - 1);
                            subset.Addresses.RemoveAt(index);
                            subset.Ports.RemoveAt(index);
                        }

                        _k8sServiceGeneration++;
                    }

                    endpoints.Metadata.Generation = _k8sServiceGeneration;
                    json = JsonConvert.SerializeObject(endpoints);
                }

                if (context.Request.Headers.TryGetValue("Authorization", out var values))
                {
                    _receivedToken = values.First();
                }

                context.Response.Headers.Append("Content-Type", "application/json");
                await context.Response.WriteAsync(json);
            }
            
            if (context.Request.Path.Value == $"/api/v1/watch/namespaces/{namespaces}/endpoints/{serviceName}")
            {
                var json = JsonConvert.SerializeObject(new ResourceEventV1<EndpointsV1>()
                {
                    EventType = ResourceEventType.Added,
                    Resource = endpoints,
                });

                if (context.Request.Headers.TryGetValue("Authorization", out var values))
                {
                    _receivedToken = values.First();
                }

                context.Response.StatusCode = 200;
                context.Response.Headers.Append("Content-Type", "application/json");
                
                await using var sw = new StreamWriter(context.Response.Body);
                await sw.WriteLineAsync(json);
                await sw.FlushAsync();
                
                // keeping open connection like kube api will slow down tests
            }
        });
    }

    private IOcelotBuilder AddKubernetes(IServiceCollection services) => services
        .Configure(_kubeClientOptionsConfigure)
        .AddOcelot().AddKubernetes(false);

    private void WithKubernetes(IServiceCollection services) => AddKubernetes(services);
    private void WithKubernetesAndRoundRobin(IServiceCollection services) => AddKubernetes(services)
        .AddCustomLoadBalancer<RoundRobinAnalyzer>(GetRoundRobinAnalyzer)
        .Services.RemoveAll<IKubeServiceCreator>().AddSingleton<IKubeServiceCreator, FakeKubeServiceCreator>();

    private int _k8sCounter, _k8sServiceGeneration;
    private static readonly object K8sCounterLocker = new();
    private RoundRobinAnalyzer _roundRobinAnalyzer;
    private RoundRobinAnalyzer GetRoundRobinAnalyzer(DownstreamRoute route, IServiceDiscoveryProvider provider)
    {
        lock (K8sCounterLocker)
        {
            return _roundRobinAnalyzer ??= new RoundRobinAnalyzerCreator().Create(route, provider)?.Data as RoundRobinAnalyzer; //??= new RoundRobinAnalyzer(provider.GetAsync, route.ServiceName);
        }
    }
}

internal class FakeKubeServiceCreator : KubeServiceCreator
{
    public FakeKubeServiceCreator(IOcelotLoggerFactory factory) : base(factory) { }

    protected override ServiceHostAndPort GetServiceHostAndPort(KubeRegistryConfiguration configuration, EndpointsV1 endpoint, EndpointSubsetV1 subset, EndpointAddressV1 address)
    {
        var ports = subset.Ports;
        var index = subset.Addresses.IndexOf(address);
        var portV1 = ports[index];
        Logger.LogDebug(() => $"K8s service with key '{configuration.KeyOfServiceInK8s}' and address {address.Ip}; Detected port is {portV1.Name}:{portV1.Port}. Total {ports.Count} ports of [{string.Join(',', ports.Select(p => p.Name))}].");
        return new ServiceHostAndPort(address.Ip, (int)portV1.Port, portV1.Name);
    }

    protected override IEnumerable<string> GetServiceTags(KubeRegistryConfiguration configuration, EndpointsV1 endpoint, EndpointSubsetV1 subset, EndpointAddressV1 address)
    {
        var tags = base.GetServiceTags(configuration, endpoint, subset, address)
            .ToList();
        long gen = endpoint.Metadata.Generation ?? 0L;
        tags.Add($"{nameof(endpoint.Metadata.Generation)}:{gen}");
        return tags;
    }
}
