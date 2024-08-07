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
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;
using System.Collections.Concurrent;
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
            .And(x => x.GivenThereIsAFakeKubernetesProvider(endpoints, serviceName, namespaces))
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
            .And(x => x.GivenThereIsAFakeKubernetesProvider(endpoints, serviceName, namespaces))
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
        ThenServiceCountersShouldMatchLeasingCounters(servicePorts);
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

        int bottom = _roundRobinAnalyzer.BottomOfConnections(),
            top = _roundRobinAnalyzer.TopOfConnections();
        ThenAllServicesCalledRealisticAmountOfTimes(bottom, top); // with unstable checkings
        ThenServiceCountersShouldMatchLeasingCounters(servicePorts);
    }

    private (EndpointsV1 Endpoints, int[] ServicePorts) ArrangeHighLoadOnKubeProviderAndRoundRobinBalancer(
        int totalServices,
        [CallerMemberName] string serviceName = nameof(ArrangeHighLoadOnKubeProviderAndRoundRobinBalancer))
    {
        const string namespaces = nameof(KubernetesServiceDiscoveryTests);
        var servicePorts = Enumerable.Repeat(0, totalServices)
            .Select(_ => PortFinder.GetRandomPort())
            .ToArray();
        var downstreamUrls = servicePorts
            .Select(port => LoopbackLocalhostUrl(port, Array.IndexOf(servicePorts, port)))
            .ToList(); // based on localhost aka loopback network interface
        var downstreams = downstreamUrls.Select(url => new Uri(url))
            .ToList();
        var downstreamResponses = downstreams
            .Select(ds => $"{serviceName}:{ds.Host}:{ds.Port}")
            .ToList();
        var subset = new EndpointSubsetV1();
        downstreams.ForEach(ds => GivenSubsetAddress(ds, subset));
        var endpoints = GivenEndpoints(subset, serviceName); // totalServices service instances with different ports
        var route = GivenRouteWithServiceName(namespaces, serviceName, nameof(RoundRobinAnalyzer)); // !!!
        var configuration = GivenKubeConfiguration(namespaces, route);
        GivenMultipleK8sProductServicesAreRunning(downstreamUrls, downstreamResponses);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunningWithServices(WithKubernetesAndRoundRobin);
        return (endpoints, servicePorts);
    }

    private void HighlyLoadOnKubeProviderAndRoundRobinBalancer(int totalRequests, int k8sGenerationNo)
    {
        // Act
        WhenIGetUrlOnTheApiGatewayMultipleTimes("/", totalRequests); // load by X parallel requests

        // Assert
        _k8sCounter.ShouldBeGreaterThanOrEqualTo(totalRequests); // integration endpoint called times
        _k8sServiceGeneration.ShouldBe(k8sGenerationNo);
        ThenAllStatusCodesShouldBe(HttpStatusCode.OK);
        ThenAllServicesShouldHaveBeenCalledTimes(totalRequests);
        _roundRobinAnalyzer.ShouldNotBeNull().Analyze();
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
                // Each offlinePerThreads-th request to integrated K8s endpoint should fail
                lock (K8sCounterLocker)
                {
                    _k8sCounter++;
                    var subset = endpoints.Subsets[0];
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
                }

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
        .AddCustomLoadBalancer<RoundRobinAnalyzer>(GetRoundRobinAnalyzer)
        .Services
        .RemoveAll<IKubeApiClient>().AddSingleton(_clientFactory)
        .RemoveAll<IKubeServiceCreator>().AddSingleton<IKubeServiceCreator, FakeKubeServiceCreator>();

    private RoundRobinAnalyzer _roundRobinAnalyzer;
    private RoundRobinAnalyzer GetRoundRobinAnalyzer(DownstreamRoute route, IServiceDiscoveryProvider provider)
    {
        lock (K8sCounterLocker)
        {
            return _roundRobinAnalyzer ??= new RoundRobinAnalyzer(provider.GetAsync, route.ServiceName);
        }
    }

    private static readonly object ServiceCountersLocker = new();
    private Dictionary<int, int> _serviceCounters;

    private static readonly object K8sCounterLocker = new();
    private int _k8sCounter, _k8sServiceGeneration;

    private void GivenK8sProductServiceIsRunning(string url, string response)
    {
        _serviceHandlers.Add(new()); // allocate single instance
        _serviceCounters = new(); // single counter
        GivenK8sProductServiceIsRunning(url, response, 0);
        _serviceCounters[0] = 0;
    }

    private void GivenMultipleK8sProductServicesAreRunning(List<string> urls, List<string> responses)
    {
        urls.ForEach(_ => _serviceHandlers.Add(new())); // allocate multiple instances
        _serviceCounters = new(urls.Count); // multiple counters
        for (int i = 0; i < urls.Count; i++)
        {
            GivenK8sProductServiceIsRunning(urls[i], responses[i], i);
            _serviceCounters[i] = 0;
        }
    }

    private void GivenK8sProductServiceIsRunning(string url, string response, int handlerIndex)
    {
        var serviceHandler = _serviceHandlers[handlerIndex];
        serviceHandler.GivenThereIsAServiceRunningOn(url, async context =>
        {
            await Task.Delay(Random.Shared.Next(5, 15)); // emulate integration delay up to 15 milliseconds
            int count = 0;
            lock (ServiceCountersLocker)
            {
                count = ++_serviceCounters[handlerIndex];
            }

            context.Response.StatusCode = (int)HttpStatusCode.OK;
            var threadResponse = string.Concat(count, ':', response);
            await context.Response.WriteAsync(threadResponse ?? ((int)HttpStatusCode.OK).ToString());
        });
    }

    private void ThenAllServicesShouldHaveBeenCalledTimes(int expected)
    {
        var sortedByIndex = _serviceCounters.OrderBy(_ => _.Key).Select(_ => _.Value).ToArray();
        var customMessage = $"All values are [{string.Join(',', sortedByIndex)}]";
        _serviceCounters.Sum(_ => _.Value).ShouldBe(expected, customMessage);
        _roundRobinAnalyzer.Events.Count.ShouldBe(expected);
    }

    private void ThenAllServicesCalledRealisticAmountOfTimes(int bottom, int top)
    {
        var sortedByIndex = _serviceCounters.OrderBy(_ => _.Key).Select(_ => _.Value).ToArray();
        var customMessage = $"{nameof(bottom)}: {bottom}\n    {nameof(top)}: {top}\n    All values are [{string.Join(',', sortedByIndex)}]";
        int sum = 0, totalSum = _serviceCounters.Sum(_ => _.Value);

        // Last services cannot be called at all, zero counters
        for (int i = 0; i < _serviceCounters.Count && sum < totalSum; i++)
        {
            int actual = _serviceCounters[i];
            actual.ShouldBeInRange(bottom, top, customMessage);
            sum += actual;
        }
    }

    private void ThenServiceCountersShouldMatchLeasingCounters(int[] ports)
    {
        var leasingCounters = _roundRobinAnalyzer.GetHostCounters();
        for (int i = 0; i < ports.Length; i++)
        {
            var host = leasingCounters.Keys.FirstOrDefault(k => k.DownstreamPort == ports[i]);
            if (host != null) // leasing info/counters can be absent because of offline service instance with exact port in unstable scenario
            {
                int counter1 = _serviceCounters[i];
                int counter2 = leasingCounters[host];
                counter1.ShouldBe(counter2, $"Port: {ports[i]}\n    Host: {host}");
            }
        }
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

    protected override IEnumerable<string> GetServiceTags(KubeRegistryConfiguration configuration, EndpointsV1 endpoint, EndpointSubsetV1 subset, EndpointAddressV1 address)
    {
        var tags = base.GetServiceTags(configuration, endpoint, subset, address)
            .ToList();
        long gen = endpoint.Metadata.Generation ?? 0L;
        tags.Add($"{nameof(endpoint.Metadata.Generation)}:{gen}");
        return tags;
    }
}

internal class RoundRobinAnalyzer : RoundRobin, ILoadBalancer
{
    public readonly ConcurrentBag<LeaseEventArgs> Events = new();

    public RoundRobinAnalyzer(Func<Task<List<Service>>> services, string serviceName)
        : base(services, serviceName)
    {
        this.Leased += Me_Leased;
    }

    private void Me_Leased(object sender, LeaseEventArgs e) => Events.Add(e);

    public const string GenerationPrefix = nameof(EndpointsV1.Metadata.Generation) + ":";

    public object Analyze()
    {
        var allGenerations = Events
            .Select(e => e.Service.Tags.FirstOrDefault(t => t.StartsWith(GenerationPrefix)))
            .Distinct().ToArray();
        var allIndices = Events.Select(e => e.ServiceIndex)
            .Distinct().ToArray();

        Dictionary<string, List<LeaseEventArgs>> eventsPerGeneration = new();
        foreach (var generation in allGenerations)
        {
            var l = Events.Where(e => e.Service.Tags.Contains(generation)).ToList();
            eventsPerGeneration.Add(generation, l);
        }

        Dictionary<string, List<int>> generationIndices = new();
        foreach (var generation in allGenerations)
        {
            var l = eventsPerGeneration[generation].Select(e => e.ServiceIndex).Distinct().ToList();
            generationIndices.Add(generation, l);
        }

        Dictionary<string, List<Lease>> generationLeases = new();
        foreach (var generation in allGenerations)
        {
            var l = eventsPerGeneration[generation].Select(e => e.Lease).ToList();
            generationLeases.Add(generation, l);
        }

        Dictionary<string, List<ServiceHostAndPort>> generationHosts = new();
        foreach (var generation in allGenerations)
        {
            var l = eventsPerGeneration[generation].Select(e => e.Lease.HostAndPort).Distinct().ToList();
            generationHosts.Add(generation, l);
        }

        Dictionary<string, List<Lease>> generationLeasesWithMaxConnections = new();
        foreach (var generation in allGenerations)
        {
            List<Lease> leases = new();
            var uniqueHosts = generationHosts[generation];
            foreach (var host in uniqueHosts)
            {
                int max = generationLeases[generation].Where(l => l == host).Max(l => l.Connections);
                Lease wanted = generationLeases[generation].Find(l => l == host && l.Connections == max);
                leases.Add(wanted);
            }

            leases = leases.OrderBy(l => l.HostAndPort.DownstreamPort).ToList();
            generationLeasesWithMaxConnections.Add(generation, leases);
        }

        return generationLeasesWithMaxConnections;
    }

    public bool HasManyServiceGenerations(int maxGeneration)
    {
        int[] generations = new int[maxGeneration + 1];
        string[] tags = new string[maxGeneration + 1];
        for (int i = 0; i < generations.Length; i++)
        {
            generations[i] = i;
            tags[i] = GenerationPrefix + i;
        }

        var all = Events
            .Select(e => e.Service.Tags.FirstOrDefault(t => t.StartsWith(GenerationPrefix)))
            .Distinct().ToArray();
        return all.All(tags.Contains);
    }

    public Dictionary<ServiceHostAndPort, int> GetHostCounters()
    {
        var hosts = Events.Select(e => e.Lease.HostAndPort).Distinct().ToList();
        return Events
            .GroupBy(e => e.Lease.HostAndPort)
            .ToDictionary(g => g.Key, g => g.Max(e => e.Lease.Connections));
    }

    public int BottomOfConnections()
    {
        var hostCounters = GetHostCounters();
        return hostCounters.Min(_ => _.Value);
    }

    public int TopOfConnections()
    {
        var hostCounters = GetHostCounters();
        return hostCounters.Max(_ => _.Value);
    }
}
