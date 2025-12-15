using KubeClient;
using KubeClient.Models;
using KubeClient.ResourceClients;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;
using Ocelot.AcceptanceTests.LoadBalancer;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Infrastructure.Extensions;
using Ocelot.LoadBalancer.Balancers;
using Ocelot.Logging;
using Ocelot.Provider.Kubernetes;
using Ocelot.Provider.Kubernetes.Interfaces;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ocelot.AcceptanceTests.ServiceDiscovery;

public sealed class KubernetesServiceDiscoveryTests : ConcurrentSteps
{
    private readonly string _kubernetesUrl;
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
    }

    [Theory]
    [InlineData(nameof(Kube))]
    [InlineData(nameof(PollKube))] // Bug 2304 -> https://github.com/ThreeMammals/Ocelot/issues/2304
    [InlineData(nameof(WatchKube))]
    public void ShouldReturnServicesFromK8s(string discoveryType)
    {
        var servicePort = PortFinder.GetRandomPort();
        var downstreamUrl = LoopbackLocalhostUrl(servicePort);
        var downstream = new Uri(downstreamUrl);
        var subsetV1 = GivenSubsetAddress(downstream);
        var endpoints = GivenEndpoints(subsetV1);
        var route = GivenRouteWithServiceName(ServiceName());
        var configuration = GivenKubeConfiguration(route, discoveryType);
        string serviceName = ServiceName(), downstreamResponse = serviceName;
        this.Given(x => GivenServiceInstanceIsRunning(downstreamUrl, downstreamResponse))
            .And(x => x.GivenThereIsAFakeKubernetesProvider(endpoints, serviceName))
            .And(_ => GivenThereIsAConfiguration(configuration))
            .And(_ => GivenOcelotIsRunning(WithKubernetes))
            .When(_ => GivenWatchReceivedEvent())
            .When(_ => WhenIGetUrlOnTheApiGateway("/"))
            .Then(_ => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(_ => ThenTheResponseBodyShouldBe($"1^:^{downstreamResponse}"))
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
        var route = GivenRouteWithServiceName();
        route.DownstreamPathTemplate = "/{url}";
        route.DownstreamScheme = downstreamScheme; // !!! Warning !!! Select port by name as scheme
        route.UpstreamPathTemplate = "/api/example/{url}";
        route.ServiceName = serviceName; // "example-web"
        var configuration = GivenKubeConfiguration(route, nameof(Kube));

        this.Given(x => GivenServiceInstanceIsRunning(downstreamUrl, nameof(ShouldReturnServicesByPortNameAsDownstreamScheme)))
            .And(x => x.GivenThereIsAFakeKubernetesProvider(endpoints, serviceName))
            .And(_ => GivenThereIsAConfiguration(configuration))
            .And(_ => GivenOcelotIsRunning(WithKubernetes))
            .When(_ => WhenIGetUrlOnTheApiGateway("/api/example/1"))
            .Then(_ => ThenTheStatusCodeShouldBe(statusCode))
            .And(_ => ThenTheResponseBodyShouldBe(downstreamScheme == "http"
                    ? "1^:^" + nameof(ShouldReturnServicesByPortNameAsDownstreamScheme)
                    : string.Empty))
            .And(x => ThenAllServicesShouldHaveBeenCalledTimes(downstreamScheme == "http" ? 1 : 0))
            .And(x => x.ThenTheTokenIs("Bearer txpc696iUhbVoudg164r93CxDTrKRVWG"))
            .BDDfy();
    }

    [Theory]
    [Trait("Bug", "2110")]
    [InlineData(1, 30, null)]
    [InlineData(2, 50, null)]
    [InlineData(3, 50, null)]
    [InlineData(4, 50, null)]
    [InlineData(5, 50, null)]
    [InlineData(6, 99, null)]
    [InlineData(7, 99, null)]
    [InlineData(8, 99, null)]
    [InlineData(9, 999, null)]
    [InlineData(10, 999, nameof(Kube))]
    [InlineData(10, 999, nameof(PollKube))]
    [InlineData(10, 999, nameof(WatchKube))]
    public void ShouldHighlyLoadOnStableKubeProvider_WithRoundRobinLoadBalancing(int totalServices, int totalRequests, string discoveryType)
    {
        // Skip in MacOS because the test is very unstable
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) // the test is stable in Linux and Windows only
            return;
        discoveryType ??= nameof(Kube);
        int zeroGeneration = 0, k8sCount = totalRequests;
        int bottom = totalRequests / totalServices,
            top = totalRequests - (bottom * totalServices) + bottom;
        var (endpoints, servicePorts) = GivenServiceDiscoveryAndLoadBalancing(totalServices, discoveryType);
        GivenThereIsAFakeKubernetesProvider(endpoints); // stable, services will not be removed from the list

        HighlyLoadOnKubeProviderAndRoundRobinBalancer(discoveryType, totalRequests, zeroGeneration, k8sCount);

        ThenAllServicesCalledRealisticAmountOfTimes(bottom, top);
        ThenServiceCountersShouldMatchLeasingCounters(_roundRobinAnalyzer, servicePorts, totalRequests);
    }

    [Theory]
    [Trait("Bug", "2110")]
    [InlineData(5, 50, 1, null)]
    [InlineData(5, 50, 2, null)]
    [InlineData(5, 50, 3, null)]
    [InlineData(5, 50, 4, nameof(Kube))]
    [InlineData(5, 50, 4, nameof(PollKube))]
    [InlineData(5, 50, 4, nameof(WatchKube))]
    public void ShouldHighlyLoadOnUnstableKubeProvider_WithRoundRobinLoadBalancing(int totalServices, int totalRequests, int k8sGeneration, string discoveryType)
    {
        // Skip in MacOS because the test is very unstable
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) // the test is stable in Linux and Windows only
            return;
        discoveryType ??= nameof(Kube);
        int failPerThreads = (totalRequests / k8sGeneration) - 1, // k8sGeneration means number of offline services
            k8sCount = totalRequests;
        var (endpoints, servicePorts) = GivenServiceDiscoveryAndLoadBalancing(totalServices, discoveryType);
        GivenThereIsAFakeKubernetesProvider(endpoints, false, k8sGeneration, failPerThreads); // false means unstable, k8sGeneration services will be removed from the list

        HighlyLoadOnKubeProviderAndRoundRobinBalancer(discoveryType, totalRequests, discoveryType == nameof(WatchKube) ? 0 : k8sGeneration, k8sCount);

        ThenAllServicesCalledOptimisticAmountOfTimes(_roundRobinAnalyzer); // with unstable checkings
        ThenServiceCountersShouldMatchLeasingCounters(_roundRobinAnalyzer, servicePorts, totalRequests);
    }

    [Theory]
    [InlineData(nameof(Kube))]
    [InlineData(nameof(PollKube))] // Bug 2304 -> https://github.com/ThreeMammals/Ocelot/issues/2304
    [InlineData(nameof(WatchKube))]
    [Trait("Feat", "2256")]
    public void ShouldReturnServicesFromK8s_AddKubernetesWithNullConfigureOptions(string discoveryType)
    {
        var servicePort = PortFinder.GetRandomPort();
        var downstreamUrl = LoopbackLocalhostUrl(servicePort);
        var downstream = new Uri(downstreamUrl);
        var subsetV1 = GivenSubsetAddress(downstream);
        var endpoints = GivenEndpoints(subsetV1);
        var route = GivenRouteWithServiceName();
        var configuration = GivenKubeConfiguration(route, discoveryType, "txpc696iUhbVoudg164r93CxDTrKRVWG");
        string serviceName = ServiceName(), downstreamResponse = serviceName;
        this.Given(x => GivenServiceInstanceIsRunning(downstreamUrl, downstreamResponse))
            .And(x => x.GivenThereIsAFakeKubernetesProvider(endpoints, serviceName))
            .And(_ => GivenThereIsAConfiguration(configuration))
            .And(_ => GivenOcelotIsRunning(AddKubernetesWithNullConfigureOptions))
            .When(_ => GivenWatchReceivedEvent())
            .When(_ => WhenIGetUrlOnTheApiGateway("/"))
            .Then(_ => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(_ => ThenTheResponseBodyShouldBe($"1^:^{downstreamResponse}"))
            .And(x => ThenAllServicesShouldHaveBeenCalledTimes(1))
            .And(x => x.ThenTheTokenIs("Bearer txpc696iUhbVoudg164r93CxDTrKRVWG"))
            .BDDfy();
    }

    [Fact]
    [Trait("Feat", "2168")]
    [Trait("PR", "2174")] // https://github.com/ThreeMammals/Ocelot/pull/2174
    public void ShouldReturnServicesFromK8s_OneWatchRequestUpdatesServicesInfo()
    {
        (EndpointsV1 endpoints, string downstreamUrl) = GetServiceInstance();
        (EndpointsV1 updatedEndpoints, string updateDownstreamUrl) = GetServiceInstance();
        ResourceEventV1<EndpointsV1>[] events =
        [
            new() { EventType = ResourceEventType.Added, Resource = endpoints },
            new() { EventType = ResourceEventType.Modified, Resource = updatedEndpoints }
        ];
        var route = GivenRouteWithServiceName();
        var configuration = GivenKubeConfiguration(route, nameof(WatchKube));
        
        string serviceName = ServiceName(), downstreamResponse = serviceName;
        var updatedDownstreamResponse = "updated_content" + serviceName;
        this.Given(x => GivenServiceInstanceIsRunning(downstreamUrl, downstreamResponse))
            .Given(x => GivenServiceInstanceIsRunning(updateDownstreamUrl, updatedDownstreamResponse))
            .And(x => x.GivenThereIsAFakeKubernetesProvider(events, serviceName))
            .And(_ => GivenThereIsAConfiguration(configuration))
            .And(_ => GivenOcelotIsRunning(WithKubernetes))
            .When(_ => GivenWatchReceivedEvent())
            .When(_ => WhenIGetUrlOnTheApiGatewayConcurrently("/", 10))
            .Then(_ => ThenAllStatusCodesShouldBe(HttpStatusCode.OK))
            .Then(_ => ThenAllResponseBodiesShouldBe(downstreamResponse))
            .And(_ => ThenK8sShouldBeCalledExactly(1))
            .And(x => ThenAllServicesShouldHaveBeenCalledTimes(10))
            .When(_ => GivenWatchReceivedEvent())
            .Given(_ => GivenDelay(100))
            .When(_ => WhenIGetUrlOnTheApiGatewayConcurrently("/", 10))
            .Then(_ => ThenAllStatusCodesShouldBe(HttpStatusCode.OK))
            .Then(_ => ThenAllResponseBodiesShouldBe(updatedDownstreamResponse))
            .And(_ => ThenK8sShouldBeCalledExactly(1))
            .And(x => ThenAllServicesShouldHaveBeenCalledTimes(20))
            .BDDfy();

        (EndpointsV1 Endpoints, string DownstreamUrl) GetServiceInstance()
        {
            var servicePort = PortFinder.GetRandomPort();
            var downstreamUrl = LoopbackLocalhostUrl(servicePort);
            var downstream = new Uri(downstreamUrl);
            var subset = GivenSubsetAddress(downstream);
            var endpoints = GivenEndpoints(subset);
            return (endpoints, downstreamUrl);
        }
    }

    [Theory]
    [Trait("Feat", "585")]
    [Trait("Feat", "2319")]
    [Trait("PR", "2324")] // https://github.com/ThreeMammals/Ocelot/pull/2324
    [InlineData(nameof(Kube))]
    [InlineData(nameof(PollKube))] // Bug 2304 -> https://github.com/ThreeMammals/Ocelot/issues/2304
    [InlineData(nameof(WatchKube))]
    public void ShouldApplyGlobalLoadBalancerOptions_ForAllDynamicRoutes(string discoveryType)
    {
        static void ConfigureDynamicRouting(FileConfiguration configuration)
        {
            configuration.GlobalConfiguration.LoadBalancerOptions = new(nameof(RoundRobin));
            configuration.GlobalConfiguration.DownstreamScheme = Uri.UriSchemeHttp;
            configuration.Routes = []; // dynamic routing
            configuration.DynamicRoutes = []; // no dynamic routes, for ALL dynamic routes
        }
        var (endpoints, servicePorts) = GivenServiceDiscoveryAndLoadBalancing(
            5, discoveryType, nameof(RoundRobin),
            ConfigureDynamicRouting,
            WithKubernetesAndFakeKubeServiceCreator);
        GivenThereIsAFakeKubernetesProvider(endpoints);
        if (discoveryType == nameof(WatchKube))
            GivenWatchReceivedEvent();

        var upstreamPath = $"/{ServiceNamespace()}.{ServiceName()}/";
        WhenIGetUrlOnTheApiGatewayConcurrently(upstreamPath, 50);

        if (discoveryType == nameof(PollKube))
        {
            if (IsCiCd()) _k8sCounter.ShouldBeInRange(48, 52);
            else _k8sCounter.ShouldBeGreaterThanOrEqualTo(50); // can be 50, 51 and sometimes 52
        }
        else
        {
            _k8sCounter.ShouldBe(discoveryType == nameof(WatchKube) ? 1 : 50);
        }

        _k8sServiceGeneration.ShouldBe(0);
        ThenAllStatusCodesShouldBe(HttpStatusCode.OK);
        ThenAllServicesShouldHaveBeenCalledTimes(50);
        ThenAllServicesCalledRealisticAmountOfTimes(9, 11); // soft assertion
        ThenServicesShouldHaveBeenCalledTimes(10, 10, 10, 10, 10); // distribution by RoundRobin algorithm, aka strict assertion
    }

    private void AddKubernetesWithNullConfigureOptions(IServiceCollection services)
        => services.AddOcelot().AddKubernetes(configureOptions: null);

    private (EndpointsV1 Endpoints, int[] ServicePorts) GivenServiceDiscoveryAndLoadBalancing(
        int totalServices,
        string discoveryType = nameof(Kube),
        string loadBalancerType = nameof(RoundRobinAnalyzer),
        Action<FileConfiguration> configure = null,
        Action<IServiceCollection> services = null,
        [CallerMemberName] string serviceName = null)
    {
        serviceName ??= ServiceName();
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
        var route = GivenRouteWithServiceName(serviceName, loadBalancerType); // !!!
        var configuration = GivenKubeConfiguration(route, discoveryType.IfEmpty(nameof(Kube)));
        configure?.Invoke(configuration);
        GivenMultipleServiceInstancesAreRunning(downstreamUrls, downstreamResponses);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(services ?? WithKubernetesAndRoundRobin);
        return (endpoints, servicePorts);
    }

    private void HighlyLoadOnKubeProviderAndRoundRobinBalancer(string discoveryType, int totalRequests, int k8sGenerationNo, int? k8sCount = null)
    {
        if (discoveryType == nameof(WatchKube))
        {
            k8sCount = GivenWatchReceivedEvent(); // 1
            k8sGenerationNo = 0;
        }

        // Act
        WhenIGetUrlOnTheApiGatewayConcurrently("/", totalRequests); // load by X parallel requests

        // Assert
        if (discoveryType == nameof(WatchKube))
            _k8sCounter.ShouldBeLessThanOrEqualTo(k8sCount ?? totalRequests); // TODO This is something abnormal due to values 997-999, but actual value should be 1. Need to double check this.
        else
            _k8sCounter.ShouldBeGreaterThanOrEqualTo(k8sCount ?? totalRequests); // integration endpoint called times

        _k8sServiceGeneration.ShouldBe(k8sGenerationNo);
        ThenAllStatusCodesShouldBe(HttpStatusCode.OK);
        ThenAllServicesShouldHaveBeenCalledTimes(totalRequests);
        _roundRobinAnalyzer.ShouldNotBeNull().Analyze();
        _roundRobinAnalyzer.Events.Count.ShouldBe(totalRequests);
        _roundRobinAnalyzer.HasManyServiceGenerations(k8sGenerationNo).ShouldBeTrue();
    }

    private void ThenTheTokenIs(string token) => _receivedToken.ShouldBe(token);
    private void ThenK8sShouldBeCalledExactly(int totalRequests) => _k8sCounter.ShouldBe(totalRequests);

    private EndpointsV1 GivenEndpoints(EndpointSubsetV1 subset, [CallerMemberName] string serviceName = "")
    {
        var e = new EndpointsV1()
        {
            Kind = "endpoint",
            ApiVersion = "1.0",
            Metadata = new()
            {
                Name = serviceName,
                Namespace = ServiceNamespace(),
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

    private FileRoute GivenRouteWithServiceName([CallerMemberName] string serviceName = null,
        string loadBalancerType = nameof(LeastConnection)) => new()
        {
            DownstreamPathTemplate = "/",
            DownstreamScheme = null, // the scheme should not be defined in service discovery scenarios by default, only ServiceName
            UpstreamPathTemplate = "/",
            UpstreamHttpMethod = [HttpMethods.Get],
            ServiceName = serviceName, // !!!
            ServiceNamespace = ServiceNamespace(),
            LoadBalancerOptions = new() { Type = loadBalancerType },
        };

    private FileConfiguration GivenKubeConfiguration(FileRoute route, string type, string token = null)
    {
        var u = new Uri(_kubernetesUrl);
        var configuration = GivenConfiguration(route);
        configuration.GlobalConfiguration.ServiceDiscoveryProvider = new()
        {
            Scheme = u.Scheme,
            Host = u.Host,
            Port = u.Port,
            Type = type,
            PollingInterval = 5 * MaxKubernetesDelay, // 3ms is very fast polling, make sense for PollKube provider only
            Namespace = ServiceNamespace(),
            Token = token ?? "Test",
        };
        return configuration;
    }

    private const int MaxKubernetesDelay = 10; // ms
    private void GivenThereIsAFakeKubernetesProvider(EndpointsV1 endpoints,
        [CallerMemberName] string serviceName = nameof(KubernetesServiceDiscoveryTests))
        => GivenThereIsAFakeKubernetesProvider(endpoints, true, 0, 0, serviceName, ServiceNamespace());

    private void GivenThereIsAFakeKubernetesProvider(EndpointsV1 endpoints, bool isStable, int offlineServicesNo, int offlinePerThreads,
        [CallerMemberName] string serviceName = null, string namespaces = null)
    {
        _k8sCounter = 0;
        serviceName ??= ServiceName();
        namespaces ??= ServiceNamespace();
        handler.GivenThereIsAServiceRunningOn(_kubernetesUrl, async context =>
        {
            await Task.Delay(Random.Shared.Next(1, MaxKubernetesDelay)); // emulate integration delay up to 10 milliseconds
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
                    json = JsonConvert.SerializeObject(endpoints, KubeResourceClient.SerializerSettings);
                }

                if (context.Request.Headers.TryGetValue("Authorization", out var values))
                {
                    _receivedToken = values.First();
                }

                context.Response.Headers.Append("Content-Type", "application/json");
                await context.Response.WriteAsync(json);
            }

            await GivenHandleWatchRequest(context, 
                [new() { EventType = ResourceEventType.Added, Resource = endpoints }],
                namespaces,
                serviceName);
        });
    }
    
    private void GivenThereIsAFakeKubernetesProvider(ResourceEventV1<EndpointsV1>[] events,
        [CallerMemberName] string serviceName = nameof(KubernetesServiceDiscoveryTests))
    {
        _k8sCounter = 0;
        var namespaces = ServiceNamespace();
        handler.GivenThereIsAServiceRunningOn(_kubernetesUrl, (c) => GivenHandleWatchRequest(c, events, namespaces, serviceName));
    }

    private int GivenWatchReceivedEvent() => _k8sWatchResetEvent.Set() ? 1 : 0;
    private static Task GivenDelay(int milliseconds) => Task.Delay(TimeSpan.FromMilliseconds(milliseconds));
    
    private async Task GivenHandleWatchRequest(HttpContext context,
        IEnumerable<ResourceEventV1<EndpointsV1>> events,
        string namespaces,
        string serviceName)
    {
        await Task.Delay(Random.Shared.Next(1, 10)); // emulate integration delay up to 10 milliseconds
            
        if (context.Request.Path.Value == $"/api/v1/watch/namespaces/{namespaces}/endpoints/{serviceName}")
        {
            _k8sCounter++;

            if (context.Request.Headers.TryGetValue("Authorization", out var values))
            {
                _receivedToken = values.First();
            }

            context.Response.StatusCode = 200;
            context.Response.Headers.Append("Content-Type", "application/json");

            foreach (var @event in events)
            {
                _k8sWatchResetEvent.WaitOne();
                var json = JsonConvert.SerializeObject(@event, KubeResourceClient.SerializerSettings);
                await using var sw = new StreamWriter(context.Response.Body);
                await sw.WriteLineAsync(json);
                await sw.FlushAsync();
                _k8sWatchResetEvent.Reset();
            }

            // keeping open connection like kube api will slow down tests
        }
    }

    private static ServiceDescriptor GetValidateScopesDescriptor()
        => ServiceDescriptor.Singleton<IServiceProviderFactory<IServiceCollection>>(
            new DefaultServiceProviderFactory(new() { ValidateScopes = true }));
    
    private IOcelotBuilder AddKubernetes(IServiceCollection services) => services
        .Replace(GetValidateScopesDescriptor())
        .AddOcelot().AddKubernetes(_kubeClientOptionsConfigure);

    private void WithKubernetes(IServiceCollection services) => AddKubernetes(services);
    private void WithKubernetesAndFakeKubeServiceCreator(IServiceCollection services) => AddKubernetes(services)
        .Services.RemoveAll<IKubeServiceCreator>().AddSingleton<IKubeServiceCreator, FakeKubeServiceCreator>();
    private void WithKubernetesAndRoundRobin(IServiceCollection services) => AddKubernetes(services)
        .AddCustomLoadBalancer<RoundRobinAnalyzer>(GetRoundRobinAnalyzer)
        .Services.RemoveAll<IKubeServiceCreator>().AddSingleton<IKubeServiceCreator, FakeKubeServiceCreator>();

    private int _k8sCounter, _k8sServiceGeneration;
    private static readonly object K8sCounterLocker = new();
    private RoundRobinAnalyzer _roundRobinAnalyzer;
    private AutoResetEvent _k8sWatchResetEvent = new(false);
    private RoundRobinAnalyzer GetRoundRobinAnalyzer(DownstreamRoute route, IServiceDiscoveryProvider provider)
    {
        lock (K8sCounterLocker)
        {
            return _roundRobinAnalyzer ??= new RoundRobinAnalyzerCreator().Create(route, provider)?.Data as RoundRobinAnalyzer; //??= new RoundRobinAnalyzer(provider.GetAsync, route.ServiceName);
        }
    }

    protected override string ServiceName([CallerMemberName] string serviceName = null) => serviceName ?? nameof(KubernetesServiceDiscoveryTests);
    protected override string ServiceNamespace() => nameof(KubernetesServiceDiscoveryTests);
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
