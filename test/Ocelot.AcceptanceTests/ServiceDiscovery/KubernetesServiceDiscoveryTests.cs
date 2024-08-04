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
using System.Collections;
using System.Collections.Concurrent;
using System.Reactive.Subjects;
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
    [InlineData(true, 0)]
    [InlineData(false, 1)]
    //[InlineData(false, 2)]
    //[InlineData(false, 3)]
    public void ShouldReturnServicesFromK8s_HighlyLoadOnTheProviderAndRoundRobinBalancer(bool isK8sGenerationStable, int offlineServicesNo)
    {
        // Arrange
        const int totalServices = 5, totalRequests = 50;
        const string namespaces = nameof(KubernetesServiceDiscoveryTests);
        const string serviceName = nameof(ShouldReturnServicesFromK8s_HighlyLoadOnTheProviderAndRoundRobinBalancer);
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
        var endpoints = GivenEndpoints(subset); // total 3 service instances with different ports
        var route = GivenRouteWithServiceName(namespaces, loadBalancerType: nameof(RoundRobinAnalyzer)); // !!!
        var configuration = GivenKubeConfiguration(namespaces, route);
        int bottom = totalRequests / totalServices,
            top = totalRequests - (bottom * totalServices) + bottom,
            failPerThreads = offlineServicesNo > 0 ? (totalRequests / offlineServicesNo) - 1 : 0;
        GivenMultipleK8sProductServicesAreRunning(downstreamUrls, downstreamResponses, totalRequests);
        GivenThereIsAFakeKubernetesProvider(serviceName, namespaces, endpoints, isK8sGenerationStable, offlineServicesNo, failPerThreads); // with stability option
        GivenThereIsAConfiguration(configuration);

        GivenOcelotIsRunningWithServices(WithKubernetesAndRoundRobin);

        // Act
        WhenIGetUrlOnTheApiGatewayMultipleTimes("/", totalRequests); // load by 50 parallel requests

        // Assert
        _k8sCounter.ShouldBe(totalRequests);
        _k8sServiceGeneration.ShouldBe(isK8sGenerationStable ? 0 : offlineServicesNo);
        ThenAllStatusCodesShouldBe(HttpStatusCode.OK);
        ThenAllServicesShouldHaveBeenCalledTimes(totalRequests);

        _roundRobinAnalyzer.ShouldNotBeNull().Analyze();
        _roundRobinAnalyzer.HasManyServiceGenerations(isK8sGenerationStable ? 0 : offlineServicesNo).ShouldBeTrue();
        ThenAllServicesCalledRealisticAmountOfTimes(
            isK8sGenerationStable ? bottom : _roundRobinAnalyzer.BottomOfConnections(),
            isK8sGenerationStable ? top : _roundRobinAnalyzer.TopOfConnections());
        ThenServiceCountersShouldMatchLeasingCounters(servicePorts);
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
        => GivenThereIsAFakeKubernetesProvider(serviceName, namespaces, endpoints, true, 0, 0);

    private void GivenThereIsAFakeKubernetesProvider(string serviceName, string namespaces, EndpointsV1 endpoints, bool isStable, int offlineServicesNo, int offlinePerThreads)
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

    private void GivenMultipleK8sProductServicesAreRunning(List<string> urls, List<string> responses, int totalThreads)
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
        var customMessage = $"{nameof(bottom)}: {bottom}\n\t{nameof(top)}: {top}\n\tAll values are [{string.Join(',', sortedByIndex)}]";
        _serviceCounters.Values.ShouldAllBe(counter => bottom <= counter && counter <= top, customMessage);
    }

    private void ThenServiceCountersShouldMatchLeasingCounters(int[] ports)
    {
        var leasingCounters = _roundRobinAnalyzer.GetHostCounters();
        for (int i = 0; i < ports.Length; i++)
        {
            int counter1 = _serviceCounters[i];
            var host = leasingCounters.Keys.Single(k => k.DownstreamPort == ports[i]);
            int counter2 = leasingCounters[host];
            counter1.ShouldBe(counter2, $"Port: {ports[i]}\n\tHost: {host}");
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
