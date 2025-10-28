using Microsoft.Extensions.DependencyInjection;
using Ocelot.AcceptanceTests.Caching;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Infrastructure.Extensions;
using Ocelot.LoadBalancer.Balancers;
using Ocelot.Metadata;
using Ocelot.ServiceDiscovery;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;

namespace Ocelot.AcceptanceTests.ServiceDiscovery;

/// <summary>
/// These tests are based on the custom service discovery provider, abstracting from currently implemented discovery providers and focusing on the dynamic routing features.
/// </summary>
public class DynamicRoutingTests : ConcurrentSteps
{
    [Fact]
    [Trait("Feat", "351")]
    public void ShouldForwardQueryStringToDownstream()
    {
        var ports = PortFinder.GetPorts(2);
        var serviceName = ServiceName();
        var serviceUrls = ports.Select(DownstreamUrl).ToArray();
        var configuration = GivenDynamicRouting(new()
        {
            { serviceName, serviceUrls },
        });
        GivenMultipleServiceInstancesAreRunning(serviceUrls);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithDiscovery);
        var pathWithQueryString = $"/{serviceName}/?{nameof(TestID)}={TestID}";
        WhenIGetUrlOnTheApiGatewayConcurrently(pathWithQueryString, 2);
        ThenAllServicesShouldHaveBeenCalledTimes(2);
        ThenServicesShouldHaveBeenCalledTimes(1, 1);
        var pathAndQuery = ThenAllResponsesHeaderExists(HeaderNames.Path).ToList();
        pathAndQuery.ShouldAllBe(pathQuery => pathWithQueryString.Contains(pathQuery));
    }

    [Fact]
    [Trait("Feat", "585")]
    [Trait("Feat", "2319")]
    [Trait("PR", "2324")] // https://github.com/ThreeMammals/Ocelot/pull/2324
    public void ShouldApplyGlobalLoadBalancerOptions_ForAllDynamicRoutes()
    {
        var ports = PortFinder.GetPorts(5);
        var serviceName = ServiceName();
        var serviceUrls = ports.Select(DownstreamUrl).ToArray();
        var configuration = GivenDynamicRouting(new()
        {
            { serviceName, serviceUrls },
        });
        GivenMultipleServiceInstancesAreRunning(serviceUrls);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithDiscovery);
        WhenIGetUrlOnTheApiGatewayConcurrently($"/{serviceName}/", 50);
        ThenAllServicesShouldHaveBeenCalledTimes(50);
        ThenAllServicesCalledRealisticAmountOfTimes(9, 11); // soft assertion
        ThenServicesShouldHaveBeenCalledTimes(10, 10, 10, 10, 10); // distribution by RoundRobin algorithm, aka strict assertion
    }

    [Fact]
    [Trait("Feat", "585")]
    [Trait("Feat", "2319")]
    [Trait("PR", "2324")] // https://github.com/ThreeMammals/Ocelot/pull/2324
    public void ShouldApplyGlobalGroupLoadBalancerOptions_ForDynamicRoutes_WhenRouteOptsHasAKey()
    {
        // 1st route
        var ports1 = PortFinder.GetPorts(2);
        var route1 = GivenLbRoute("route1");
        route1.LoadBalancerOptions = null; // 1st route is not balanced
        route1.Key = null; // 1st route is not in the global group
        route1.Metadata = new Dictionary<string, string>()
        {
            { route1.ServiceName, ports1.Select(DownstreamUrl).Csv() },
        };

        // 2nd route
        var ports2 = PortFinder.GetPorts(2);
        var route2 = GivenLbRoute("route2");
        route2.LoadBalancerOptions = null; // 2nd route opts will be applied from global ones
        route2.Key = "R2"; // 2nd route is in the group
        route2.Metadata = new Dictionary<string, string>()
        {
            { route2.ServiceName, ports2.Select(DownstreamUrl).Csv() },
        };

        // 3rd route
        var ports3 = PortFinder.GetPorts(2);
        var route3 = GivenLbRoute("noLoadBalancing", loadBalancer: nameof(NoLoadBalancer));
        route3.Metadata = new Dictionary<string, string>()
        {
            { route3.ServiceName, ports3.Select(DownstreamUrl).Csv() },
        };

        var configuration = GivenDynamicRouting(new(), route1, route2, route3);
        configuration.GlobalConfiguration.LoadBalancerOptions = new()
        {
            RouteKeys = ["R2"],
            Type = nameof(RoundRobin),
        };

        var downstreamUrls = ports1.Union(ports2).Union(ports3).Select(DownstreamUrl).ToArray();
        GivenMultipleServiceInstancesAreRunning(downstreamUrls);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithDiscovery);

        WhenIGetUrlOnTheApiGatewayConcurrently("/route1/", 2);
        WhenIGetUrlOnTheApiGatewayConcurrently("/route2/", 4);
        WhenIGetUrlOnTheApiGatewayConcurrently("/noLoadBalancing/", 5);
        ThenServicesShouldHaveBeenCalledTimes(2, 0, 2, 2, 5, 0); // main assertion, explanation is below
        ThenServiceShouldHaveBeenCalledTimes(0, 2); // NoLoadBalancer for 2
        ThenServiceShouldHaveBeenCalledTimes(1, 0); // NoLoadBalancer for 2
        ThenServiceShouldHaveBeenCalledTimes(2, 2); // RoundRobin for 4
        ThenServiceShouldHaveBeenCalledTimes(3, 2); // RoundRobin for 4
        ThenServiceShouldHaveBeenCalledTimes(4, 5); // NoLoadBalancer for 5
        ThenServiceShouldHaveBeenCalledTimes(5, 0); // NoLoadBalancer for 5
    }

    [Fact]
    [Trait("Feat", "585")]
    [Trait("PR", "2324")] // https://github.com/ThreeMammals/Ocelot/pull/2324
    public void ShouldApplyGlobalCacheOptions_ForAllDynamicRoutes()
    {
        const int TTL = 1; // let's cache for one second
        var ports = PortFinder.GetPorts(2);
        var serviceName = ServiceName();
        var serviceUrls = ports.Select(DownstreamUrl).ToArray();
        var configuration = GivenDynamicRouting(new()
        {
            { serviceName, serviceUrls },
        });
        configuration.GlobalConfiguration.CacheOptions = new()
        {
            TtlSeconds = TTL, // Let's cache for one second
        };

        var (testBody1, testBody2) = CachingTests.TestBodiesFactory();
        GivenMultipleServiceInstancesAreRunning(serviceUrls, responses: [testBody1, testBody2]);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithDiscovery);
        AssertCachedRoute(TTL, serviceName, ports, [testBody1, testBody2]);
    }

    private void AssertCachedRoute(int ttl, string serviceName, int[] ports, string[] expectedBody, bool cached = true, bool balanced = true, int shift = 0)
    {
        Array.Clear(_counters);
        var url = $"/{serviceName}/";
        WhenIGetUrlOnTheApiGatewayConcurrently(url, 2);
        ThenAllServicesShouldHaveBeenCalledTimes(2);

        //ThenServicesShouldHaveBeenCalledTimes(1, 1); // distribution by RoundRobin algorithm, aka strict assertion
        ThenServiceShouldHaveBeenCalledTimes(shift + 0, balanced ? 1 : 2);
        ThenServiceShouldHaveBeenCalledTimes(shift + 1, balanced ? 1 : 0);

        GivenIWait(100);
        WhenIGetUrlOnTheApiGatewayConcurrently(url, 2);
        ThenAllServicesShouldHaveBeenCalledTimes(cached ? 2 : 4); // the counters remain unchanged, and the items are still in the cache
        int counter = cached ? 1 : 2;

        //ThenServicesShouldHaveBeenCalledTimes(counter, counter); // the counters remain unchanged
        ThenServiceShouldHaveBeenCalledTimes(shift + 0, balanced ? counter : 2 * counter);
        ThenServiceShouldHaveBeenCalledTimes(shift + 1, balanced ? counter : 0);

        GivenIWait(ttl * 1000); // allow cached items to expire
        WhenIGetUrlOnTheApiGatewayConcurrently(url, 2);
        ThenAllServicesShouldHaveBeenCalledTimes(cached ? 4 : 6); // the counters have been updated because new items were added to the cache
        counter = cached ? 2 : 3;

        //ThenServicesShouldHaveBeenCalledTimes(counter, counter); // the counters have been updated
        ThenServiceShouldHaveBeenCalledTimes(shift + 0, balanced ? counter : 2 * counter);
        ThenServiceShouldHaveBeenCalledTimes(shift + 1, balanced ? counter : 0);

        ThenAllResponseBodiesShouldBe(ports, expectedBody);
    }

    [Fact]
    [Trait("Feat", "585")]
    [Trait("PR", "2324")] // https://github.com/ThreeMammals/Ocelot/pull/2324
    public void ShouldApplyGlobalGroupCacheOptions_WhenRouteOptsHasAKey()
    {
        const int TTL = 1; // let's cache for one second

        // 1st route
        var ports1 = PortFinder.GetPorts(2);
        var route1 = GivenLbRoute("route1");
        route1.CacheOptions = null; // 1st route is not cached
        route1.Key = null; // 1st route is not in the global group
        route1.Metadata = new Dictionary<string, string>()
        {
            { route1.ServiceName, ports1.Select(DownstreamUrl).Csv() },
        };

        // 2nd route
        var ports2 = PortFinder.GetPorts(2);
        var route2 = GivenLbRoute("route2");
        route2.CacheOptions = null; // 2nd route opts will be applied from global ones
        route2.Key = "R2"; // 2nd route is in the group
        route2.Metadata = new Dictionary<string, string>()
        {
            { route2.ServiceName, ports2.Select(DownstreamUrl).Csv() },
        };

        // 3rd route
        var ports3 = PortFinder.GetPorts(2);
        var route3 = GivenLbRoute("noCaching", loadBalancer: nameof(NoLoadBalancer));
        route3.Metadata = new Dictionary<string, string>()
        {
            { route3.ServiceName, ports3.Select(DownstreamUrl).Csv() },
        };

        var configuration = GivenDynamicRouting(new(), route1, route2, route3);
        configuration.GlobalConfiguration.CacheOptions = new()
        {
            RouteKeys = ["R2"],
            Region = "global",
            Header = "global",
            TtlSeconds = TTL,
        };

        var downstreamUrls = ports1.Union(ports2).Union(ports3).Select(DownstreamUrl).ToArray();
        var (testBody1, testBody2) = CachingTests.TestBodiesFactory();
        GivenMultipleServiceInstancesAreRunning(downstreamUrls, responses: [testBody1, testBody2, testBody1, testBody2, testBody1, testBody2]);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithDiscovery);
        int length = _counters.Length;
        AssertCachedRoute(TTL, route1.ServiceName, ports1, [testBody1, testBody2], cached: false, shift: 0);
        int[] counters1 = new int[length];
        Array.Copy(_counters, counters1, length);

        AssertCachedRoute(TTL, route2.ServiceName, ports2, [testBody1, testBody2], cached: true, shift: 2);
        int[] counters2 = new int[length];
        Array.Copy(_counters, counters2, length);

        AssertCachedRoute(TTL, route3.ServiceName, ports3, [testBody1, testBody2], cached: false, balanced: false, shift: 4);
        int[] counters3 = new int[length];
        Array.Copy(_counters, counters3, length);

        for (int i = 0; i < length; i++)
        {
            _counters[i] = counters1[i] + counters2[i] + counters3[i];
        }
        ThenServicesShouldHaveBeenCalledTimes(3, 3, 2, 2, 6, 0); // main assertion, explanation is below
        ThenServiceShouldHaveBeenCalledTimes(0, 3); // RoundRobin for 6, not cached
        ThenServiceShouldHaveBeenCalledTimes(1, 3); // RoundRobin for 6, not cached
        ThenServiceShouldHaveBeenCalledTimes(2, 2); // RoundRobin for 6, cached 1
        ThenServiceShouldHaveBeenCalledTimes(3, 2); // RoundRobin for 6, cached 1
        ThenServiceShouldHaveBeenCalledTimes(4, 6); // NoLoadBalancer for 6, not cached
        ThenServiceShouldHaveBeenCalledTimes(5, 0); // NoLoadBalancer for 6, not cached
    }

    private FileConfiguration GivenDynamicRouting(Dictionary<string, IEnumerable<string>> services, params FileDynamicRoute[] routes)
    {
        var config = new FileConfiguration()
        {
            DynamicRoutes = new(routes),
            GlobalConfiguration = new()
            {
                DownstreamScheme = Uri.UriSchemeHttp,
                ServiceDiscoveryProvider = new()
                {
                    Type = nameof(DynamicRoutingDiscoveryProvider),
                    Host = "doesn't matter for this provider", // it should not be empty due to DownstreamRouteProviderFactory.Get
                    Port = 1, // see DownstreamRouteProviderFactory.IsServiceDiscovery
                },
                LoadBalancerOptions = new(nameof(RoundRobin)),
            },
        };
        config.GlobalConfiguration.Metadata = services.ToDictionary(x => x.Key, x => x.Value.Csv());
        return config;
    }

    private FileDynamicRoute GivenLbRoute(string serviceName, string serviceNamespace = null, string loadBalancer = null)
    {
        var route = new FileDynamicRoute()
        {
            ServiceName = serviceName,
            ServiceNamespace = serviceNamespace ?? ServiceNamespace(),
            LoadBalancerOptions = new(loadBalancer ?? nameof(RoundRobin)),
        };
        return route;
    }

    private static readonly ServiceDiscoveryFinderDelegate DynamicRoutingDiscoveryFinder = (provider, config, route)
        => new DynamicRoutingDiscoveryProvider(provider, config, route);
    private static void WithDiscovery(IServiceCollection services) => services
        .AddSingleton(DynamicRoutingDiscoveryFinder)
        .AddOcelot();

    protected override string ServiceNamespace() => nameof(DynamicRoutingTests);
}

public class DynamicRoutingDiscoveryProvider : IServiceDiscoveryProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ServiceProviderConfiguration _config;
    private readonly DownstreamRoute _downstreamRoute;

    public DynamicRoutingDiscoveryProvider(IServiceProvider serviceProvider, ServiceProviderConfiguration config, DownstreamRoute downstreamRoute)
    {
        _serviceProvider = serviceProvider;
        _config = config;
        _downstreamRoute = downstreamRoute;
    }

    public Task<List<Service>> GetAsync()
    {
        if (!_downstreamRoute.MetadataOptions.Metadata.TryGetValue(_downstreamRoute.ServiceName, out var data)
            || data.IsEmpty()) 
            return Task.FromResult<List<Service>>(new());

        var urls = _downstreamRoute
            .GetMetadata<string[]>(_downstreamRoute.ServiceName)
            .Select(x => new Uri(x))
            .ToList();
        var services = urls
            .Select(url => new Service(
                name: _downstreamRoute.ServiceName,
                hostAndPort: new(url.Host, url.Port, url.Scheme.IfEmpty(_downstreamRoute.DownstreamScheme)),
                id: $"{_downstreamRoute.ServiceNamespace}.{_downstreamRoute.ServiceName}",
                version: DateTime.UtcNow.ToString("O"),
                tags: Enumerable.Empty<string>()))
            .ToList();
        return Task.FromResult(services);
    }
}
