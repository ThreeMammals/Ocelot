using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Acceptance.Caching;
using Ocelot.Acceptance.QualityOfService;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.LoadBalancer.Balancers;
using Ocelot.Logging;
using Ocelot.Requester;
using Ocelot.Testing.Authentication;
using Ocelot.Testing.Requester;
using Ocelot.Testing.Steps;
using System.Net.Mime;
using System.Runtime.CompilerServices;

namespace Ocelot.Acceptance.ServiceDiscovery;

/// <summary>
/// These tests are based on the custom service discovery provider, abstracting from currently implemented discovery providers and focusing on the dynamic routing features.
/// </summary>
public class DynamicRoutingTests : DiscoverySteps
{
    public const bool EnabledDiscovery = true;

    [Fact]
    [Trait("Feat", "351")] // https://github.com/ThreeMammals/Ocelot/pull/351
    public async Task ShouldForwardQueryStringToDownstream()
    {
        var ports = PortFinder.GetPorts(2);
        var serviceName = ServiceName();
        var serviceUrls = ports.Select(DownstreamUrl).ToArray();
        var configuration = GivenDynamicRouting(new()
        {
            { serviceName, serviceUrls },
        });
        var pathWithQueryString = $"/{serviceName}/?{nameof(TestID)}={TestID}";
        GivenMultipleServiceInstancesAreRunning(serviceUrls, serviceName);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithDiscovery);
        await Task.WhenAll(
            WhenIGetUrlOnTheApiGatewayConcurrently(pathWithQueryString, 2));
        ThenAllServicesShouldHaveBeenCalledTimes(2);
        ThenServicesShouldHaveBeenCalledTimes(1, 1);
        ThenAllResponsesHeaderExists(HeaderNames.Path);
        ThenAllResponsesPathAndQueryShouldAllBeContainedInThePath(pathWithQueryString);
    }
    private void ThenAllResponsesPathAndQueryShouldAllBeContainedInThePath(string pathWithQueryString)
    {
        var pathAndQuery = ThenAllResponsesHeaderExists(HeaderNames.Path).ToList();
        pathAndQuery.ShouldAllBe(pathQuery => pathWithQueryString.Contains(pathQuery));
    }

    [Fact]
    [Trait("Feat", "585")] // https://github.com/ThreeMammals/Ocelot/issues/585
    [Trait("Feat", "2319")] // https://github.com/ThreeMammals/Ocelot/issues/2319
    [Trait("PR", "2324")] // https://github.com/ThreeMammals/Ocelot/pull/2324
    public async Task ShouldApplyGlobalLoadBalancerOptionsForAllDynamicRoutes()
    {
        var ports = PortFinder.GetPorts(5);
        var serviceName = ServiceName();
        var serviceUrls = ports.Select(DownstreamUrl).ToArray();
        var configuration = GivenDynamicRouting(new()
        {
            { serviceName, serviceUrls },
        });
        GivenMultipleServiceInstancesAreRunning(serviceUrls, serviceName);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithDiscovery);
        await Task.WhenAll(
            WhenIGetUrlOnTheApiGatewayConcurrently(UpstreamPath(), 50));
        ThenAllServicesShouldHaveBeenCalledTimes(50);
        ThenAllServicesCalledRealisticAmountOfTimes(9, 11); // soft assertion
        ThenServicesShouldHaveBeenCalledTimes(10, 10, 10, 10, 10); // distribution by RoundRobin algorithm, aka strict assertion
    }

    [Fact]
    [Trait("Feat", "585")] // https://github.com/ThreeMammals/Ocelot/issues/585
    [Trait("Feat", "2319")] // https://github.com/ThreeMammals/Ocelot/issues/2319
    [Trait("PR", "2324")] // https://github.com/ThreeMammals/Ocelot/pull/2324
    public async Task ShouldApplyGlobalGroupLoadBalancerOptionsForDynamicRoutesWhenRouteOptsHasAKey()
    {
        // 1st route
        var ports1 = PortFinder.GetPorts(2);
        var route1 = GivenLbRoute("route1", key: null); // 1st route is not in the global group
        route1.LoadBalancerOptions = null; // 1st route is not balanced
        GivenDiscoveryMetadata(route1, ports1);

        // 2nd route
        var ports2 = PortFinder.GetPorts(2);
        var route2 = GivenLbRoute("route2", key: "R2"); // 2nd route is in the group
        route2.LoadBalancerOptions = null; // 2nd route opts will be applied from global ones
        GivenDiscoveryMetadata(route2, ports2);

        // 3rd route
        var ports3 = PortFinder.GetPorts(2);
        var route3 = GivenLbRoute("noLoadBalancing", loadBalancer: nameof(NoLoadBalancer), key: null);
        GivenDiscoveryMetadata(route3, ports3);

        var configuration = GivenDynamicRouting(new(), route1, route2, route3);
        configuration.GlobalConfiguration.LoadBalancerOptions = new()
        {
            RouteKeys = ["R2"],
            Type = nameof(RoundRobin),
        };

        var downstreamUrls = ports1.Union(ports2).Union(ports3).Select(DownstreamUrl).ToArray();
        var serviceName = ServiceName();
        GivenMultipleServiceInstancesAreRunning(downstreamUrls, serviceName);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithDiscovery);
        await Task.WhenAll(
            WhenIGetUrlOnTheApiGatewayConcurrently("/route1/", 2));
        await Task.WhenAll(
            WhenIGetUrlOnTheApiGatewayConcurrently("/route2/", 4));
        await Task.WhenAll(
            WhenIGetUrlOnTheApiGatewayConcurrently("/noLoadBalancing/", 5));
        ThenServicesShouldHaveBeenCalledTimes(2, 0, 2, 2, 5, 0); // main assertion, explanation is below
        ThenServiceShouldHaveBeenCalledTimes(0, 2); // NoLoadBalancer for 2
        ThenServiceShouldHaveBeenCalledTimes(1, 0); // NoLoadBalancer for 2
        ThenServiceShouldHaveBeenCalledTimes(2, 2); // RoundRobin for 4
        ThenServiceShouldHaveBeenCalledTimes(3, 2); // RoundRobin for 4
        ThenServiceShouldHaveBeenCalledTimes(4, 5); // NoLoadBalancer for 5
        ThenServiceShouldHaveBeenCalledTimes(5, 0); // NoLoadBalancer for 5
    }

    [Fact]
    [Trait("Feat", "585")] // https://github.com/ThreeMammals/Ocelot/issues/585
    [Trait("Feat", "2330")] // https://github.com/ThreeMammals/Ocelot/issues/2330
    [Trait("PR", "2331")] // https://github.com/ThreeMammals/Ocelot/pull/2331
    public Task ShouldApplyGlobalCacheOptionsForAllDynamicRoutes()
    {
        const int TTL = 1; // let's cache for one second
        var ports = PortFinder.GetPorts(2);
        var serviceName = ServiceName();
        var serviceUrls = ports.Select(DownstreamUrl).ToArray();
        var configuration = GivenDynamicRouting(new()
        {
            { serviceName, serviceUrls },
        });
        configuration.GlobalConfiguration.CacheOptions = new(TTL); // let's cache for one second

        var (testBody1, testBody2) = CachingTests.TestBodiesFactory();
        string[] responses = [testBody1, testBody2];
        GivenMultipleServiceInstancesAreRunning(serviceUrls, responses);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithDiscovery);
        return AssertCachedRoute(TTL, serviceName, ports, [testBody1, testBody2]);
    }

    private async Task AssertCachedRoute(int ttl, string serviceName, int[] ports, string[] expectedBody,
        bool cached = true, bool balanced = true, int shift = 0)
    {
        var url = UpstreamPath("/", serviceName);
        int counter = cached ? 1 : 2;
        void GivenCounterIs(int v) => counter = v;
        Array.Clear(Counters);
        await Task.WhenAll(
            WhenIGetUrlOnTheApiGatewayConcurrently(url, 2));
        ThenAllServicesShouldHaveBeenCalledTimes(2);

        // ThenServicesShouldHaveBeenCalledTimes(1, 1); // distribution by RoundRobin algorithm, aka strict assertion
        ThenServiceShouldHaveBeenCalledTimes(shift + 0, balanced ? 1 : 2);
        ThenServiceShouldHaveBeenCalledTimes(shift + 1, balanced ? 1 : 0);

        await GivenIWaitAsync(100);
        await Task.WhenAll(
            WhenIGetUrlOnTheApiGatewayConcurrently(url, 2));
        ThenAllServicesShouldHaveBeenCalledTimes(cached ? 2 : 4); // the counters remain unchanged, and the items are still in the cache

        // ThenServicesShouldHaveBeenCalledTimes(counter, counter); // the counters remain unchanged
        ThenServiceShouldHaveBeenCalledTimes(shift + 0, balanced ? counter : 2 * counter);
        ThenServiceShouldHaveBeenCalledTimes(shift + 1, balanced ? counter : 0);

        await GivenIWaitAsync(ttl * 1000); // allow cached items to expire
        await Task.WhenAll(
            WhenIGetUrlOnTheApiGatewayConcurrently(url, 2));
        ThenAllServicesShouldHaveBeenCalledTimes(cached ? 4 : 6); // the counters have been updated because new items were added to the cache
        GivenCounterIs(cached ? 2 : 3);

        // ThenServicesShouldHaveBeenCalledTimes(counter, counter); // the counters have been updated
        ThenServiceShouldHaveBeenCalledTimes(shift + 0, balanced ? counter : 2 * counter);
        ThenServiceShouldHaveBeenCalledTimes(shift + 1, balanced ? counter : 0);
        ThenAllResponseBodiesShouldBe(ports, expectedBody);
    }

    [Fact]
    [Trait("Feat", "585")] // https://github.com/ThreeMammals/Ocelot/issues/585
    [Trait("Feat", "2330")] // https://github.com/ThreeMammals/Ocelot/issues/2330
    [Trait("PR", "2331")] // https://github.com/ThreeMammals/Ocelot/pull/2331
    public async Task ShouldApplyGlobalGroupCacheOptionsWhenRouteOptsHasAKey()
    {
        const int TTL = 1; // let's cache for one second

        // 1st route
        var ports1 = PortFinder.GetPorts(2);
        var route1 = GivenLbRoute("route1", key: null); // 1st route is not in the global group
        route1.CacheOptions = null; // 1st route is not cached
        GivenDiscoveryMetadata(route1, ports1);

        // 2nd route
        var ports2 = PortFinder.GetPorts(2);
        var route2 = GivenLbRoute("route2", key: "R2"); // 2nd route is in the group
        route2.CacheOptions = null; // 2nd route opts will be applied from global ones
        GivenDiscoveryMetadata(route2, ports2);

        // 3rd route
        var ports3 = PortFinder.GetPorts(2);
        var route3 = GivenLbRoute("noCaching", loadBalancer: nameof(NoLoadBalancer), key: null);
        GivenDiscoveryMetadata(route3, ports3);

        var configuration = GivenDynamicRouting([], route1, route2, route3);
        configuration.GlobalConfiguration.CacheOptions = new()
        {
            RouteKeys = ["R2"],
            Region = "global",
            Header = "global",
            TtlSeconds = TTL,
        };

        var downstreamUrls = ports1.Union(ports2).Union(ports3).Select(DownstreamUrl).ToArray();
        var (testBody1, testBody2) = CachingTests.TestBodiesFactory();
        string[] responses = [testBody1, testBody2, testBody1, testBody2, testBody1, testBody2];
        GivenMultipleServiceInstancesAreRunning(downstreamUrls, responses);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithDiscovery);
        await AssertCachedRoute(TTL, route1.ServiceName, ports1, [testBody1, testBody2], cached: false, shift: 0);
        ThenICopyCounters1();
        await AssertCachedRoute(TTL, route2.ServiceName, ports2, [testBody1, testBody2], cached: true, shift: 2);
        ThenICopyCounters2();
        await AssertCachedRoute(TTL, route3.ServiceName, ports3, [testBody1, testBody2], cached: false, balanced: false, shift: 4);
        ThenICopyCounters3();
        SumCountersPerIndex();
        ThenServicesShouldHaveBeenCalledTimes(3, 3, 2, 2, 6, 0); // main assertion, explanation is below
        ThenServiceShouldHaveBeenCalledTimes(0, 3); // RoundRobin for 6, not cached
        ThenServiceShouldHaveBeenCalledTimes(1, 3); // RoundRobin for 6, not cached
        ThenServiceShouldHaveBeenCalledTimes(2, 2); // RoundRobin for 6, cached 1
        ThenServiceShouldHaveBeenCalledTimes(3, 2); // RoundRobin for 6, cached 1
        ThenServiceShouldHaveBeenCalledTimes(4, 6); // NoLoadBalancer for 6, not cached
        ThenServiceShouldHaveBeenCalledTimes(5, 0); // NoLoadBalancer for 6, not cached
    }
    private int[] _counters1, _counters2, _counters3;
    private void ThenICopyCounters1()
    {
        _counters1 = new int[Counters.Length];
        Array.Copy(Counters, _counters1, Counters.Length);
    }
    private void ThenICopyCounters2()
    {
        _counters2 = new int[Counters.Length];
        Array.Copy(Counters, _counters2, Counters.Length);
    }
    private void ThenICopyCounters3()
    {
        _counters3 = new int[Counters.Length];
        Array.Copy(Counters, _counters3, Counters.Length);
    }
    private void SumCountersPerIndex()
    {
        for (int i = 0; i < Counters.Length; i++)
        {
            Counters[i] = _counters1[i] + _counters2[i] + _counters3[i];
        }
    }

    [Fact]
    [Trait("Feat", "585")] // https://github.com/ThreeMammals/Ocelot/issues/585
    [Trait("Feat", "2320")] // https://github.com/ThreeMammals/Ocelot/issues/2320
    [Trait("PR", "2332")] // https://github.com/ThreeMammals/Ocelot/pull/2332
    public async Task ShouldApplyGlobalHttpHandlerOptionsForAllDynamicRoutes()
    {
        var ports = PortFinder.GetPorts(3);
        int times = ports.Length;
        var serviceName = ServiceName();
        var serviceUrls = ports.Select(DownstreamUrl).ToArray();
        var configuration = GivenDynamicRouting(new()
        {
            { serviceName, serviceUrls },
        });
        configuration.GlobalConfiguration.HttpHandlerOptions = new()
        {
            MaxConnectionsPerServer = 77,
            PooledConnectionLifetimeSeconds = 88,
            UseTracing = true, // let's enable global tracing
        };
        GivenMultipleServiceInstancesAreRunning(serviceUrls, serviceName);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithDiscoveryAndRequesterTesting);
        await Task.WhenAll(
            WhenIGetUrlOnTheApiGatewayConcurrently(UpstreamPath(), times));
        ThenAllServicesShouldHaveBeenCalledTimes(times);
        ThenServicesShouldHaveBeenCalledTimes(1, 1, 1); // distribution by RoundRobin algorithm, aka strict assertion
        ThenRouteHttpHandlerOptionsAre(serviceName, configuration.GlobalConfiguration.Metadata, 77, 88, true);
    }

    [Fact]
    [Trait("Feat", "585")] // https://github.com/ThreeMammals/Ocelot/issues/585
    [Trait("Feat", "2320")] // https://github.com/ThreeMammals/Ocelot/issues/2320
    [Trait("PR", "2332")] // https://github.com/ThreeMammals/Ocelot/pull/2332
    public async Task ShouldApplyGlobalGroupHttpHandlerOptionsForDynamicRoutesWhenRouteOptsHasAKey()
    {
        var serviceName = ServiceName();
        // 1st route
        var ports1 = PortFinder.GetPorts(2);
        var route1 = GivenLbRoute("route1", key: null); // 1st route is not in the global group
        route1.HttpHandlerOptions = null; // 1st route has no opts
        GivenDiscoveryMetadata(route1, ports1);

        // 2nd route
        var ports2 = PortFinder.GetPorts(2);
        var route2 = GivenLbRoute("route2", key: "R2"); // 2nd route is in the group
        route2.HttpHandlerOptions = null; // 2nd route opts will be applied from global ones
        GivenDiscoveryMetadata(route2, ports2);

        // 3rd route
        var ports3 = PortFinder.GetPorts(2);
        var route3 = GivenLbRoute("noTracing", loadBalancer: nameof(NoLoadBalancer), key: null);
        var route3Opts = route3.HttpHandlerOptions = new()
        {
            MaxConnectionsPerServer = 66,
            PooledConnectionLifetimeSeconds = 77,
            UseTracing = false, // no tracing route
        };
        GivenDiscoveryMetadata(route3, ports3);

        var configuration = GivenDynamicRouting(new(), route1, route2, route3);
        var globalOpts = configuration.GlobalConfiguration.HttpHandlerOptions = new()
        {
            RouteKeys = ["R2"],
            MaxConnectionsPerServer = 88,
            PooledConnectionLifetimeSeconds = 99,
            UseCookieContainer = false,
            UseProxy = false,
            UseTracing = true, // enable global tracing
        };
        var downstreamUrls = ports1.Union(ports2).Union(ports3).Select(DownstreamUrl).ToArray();

        GivenMultipleServiceInstancesAreRunning(downstreamUrls, serviceName);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithDiscoveryAndRequesterTesting);
        await Task.WhenAll(
            WhenIGetUrlOnTheApiGatewayConcurrently("/route1/", 2));
        await Task.WhenAll(
            WhenIGetUrlOnTheApiGatewayConcurrently("/route2/", 2));
        await Task.WhenAll(
            WhenIGetUrlOnTheApiGatewayConcurrently("/noTracing/", 2));
        ThenServicesShouldHaveBeenCalledTimes(1, 1, 1, 1, 2, 0);
        ThenRouteHttpHandlerOptionsAre(route1.ServiceName, route1.Metadata,
            int.MaxValue, HttpHandlerOptions.DefaultPooledConnectionLifetimeSeconds, false); // default opts
        ThenRouteHttpHandlerOptionsAre(route2.ServiceName, route2.Metadata,
            globalOpts.MaxConnectionsPerServer.Value, globalOpts.PooledConnectionLifetimeSeconds.Value, globalOpts.UseTracing.Value); // global opts
        ThenRouteHttpHandlerOptionsAre(route3.ServiceName, route3.Metadata,
            route3Opts.MaxConnectionsPerServer.Value, route3Opts.PooledConnectionLifetimeSeconds.Value, route3Opts.UseTracing.Value); // route opts
    }

    [Fact]
    [Trait("Feat", "585")] // https://github.com/ThreeMammals/Ocelot/issues/585
    [Trait("Feat", "2316")] // https://github.com/ThreeMammals/Ocelot/issues/2316
    [Trait("PR", "2336")] // https://github.com/ThreeMammals/Ocelot/pull/2336
    public async Task ShouldApplyGlobalAuthenticationOptionsForAllDynamicRoutes()
    {
        using var steps = new AuthenticationSteps();
        var ports = PortFinder.GetPorts(3);
        int times = ports.Length;
        var serviceName = ServiceName();
        var serviceUrls = ports.Select(DownstreamUrl).ToArray();
        var configuration = GivenDynamicRouting(new()
        {
            { serviceName, serviceUrls },
        });
        configuration.GlobalConfiguration.AuthenticationOptions = new(AuthenticationSteps.GivenOptions(false, ["apiGlobal"], [JwtBearerDefaults.AuthenticationScheme]));
        string[] scopes = ["apiGlobal"];
        var responses = Enumerable.Repeat(serviceName, ports.Length).ToArray();
        GivenMultipleServiceInstancesAreRunning(serviceUrls, responses);
        steps.GivenThereIsAConfiguration(configuration);
        steps.GivenOcelotIsRunning(WithDiscoveryAndJwtBearerAuthentication(steps));
        await steps.GivenThereIsExternalJwtSigningService(scopes, CancelMe);
        await steps.GivenIHaveAToken(scopes[0], null, null, null, serviceName); //,audience: ocelotClient.BaseAddress.Authority);
        steps.GivenIHaveAddedATokenToMyRequest();
        GivenIEnsureOcelotClient(steps);
        await Task.WhenAll(
            WhenIGetUrlOnTheApiGatewayConcurrently(UpstreamPath(), times));
        ThenAllServicesShouldHaveBeenCalledTimes(times);
        ThenServicesShouldHaveBeenCalledTimes(1, 1, 1); // distribution by RoundRobin algorithm, aka strict assertion
        ThenAllStatusCodesShouldBe(HttpStatusCode.OK);
        ThenAllResponseBodiesShouldBe(serviceName);
    }
    private void GivenIEnsureOcelotClient(AuthenticationSteps steps)
        => ocelotClient ??= steps.OcelotClient;

    [Fact]
    [Trait("Feat", "585")] // https://github.com/ThreeMammals/Ocelot/issues/585
    [Trait("Feat", "2316")] // https://github.com/ThreeMammals/Ocelot/issues/2316
    [Trait("PR", "2336")] // https://github.com/ThreeMammals/Ocelot/pull/2336
    public async Task ShouldApplyGlobalGroupAuthenticationOptionsForDynamicRoutesWhenRouteOptsHasAKey()
    {
        using var steps = new AuthenticationSteps();

        // 1st route
        var ports1 = PortFinder.GetPorts(2);
        var route1 = GivenLbRoute("route1", key: null); // 1st route is not in the global group
        route1.AuthenticationOptions = null; // 1st route has no opts
        GivenDiscoveryMetadata(route1, ports1);

        // 2nd route
        var ports2 = PortFinder.GetPorts(2);
        var route2 = GivenLbRoute("route2", key: "R2"); // 2nd route is in the group
        route2.AuthenticationOptions = null; // 2nd route opts will be applied from global ones
        GivenDiscoveryMetadata(route2, ports2);

        // 3rd route
        var ports3 = PortFinder.GetPorts(2);
        var route3 = GivenLbRoute("noAuthorization", loadBalancer: nameof(NoLoadBalancer), key: null);
        var route3Opts = route3.AuthenticationOptions =
            AuthenticationSteps.GivenOptions(false, ["invalid-scope"], [JwtBearerDefaults.AuthenticationScheme]);
        GivenDiscoveryMetadata(route3, ports3);

        var configuration = GivenDynamicRouting(new(), route1, route2, route3);
        var globalOptions = configuration.GlobalConfiguration.AuthenticationOptions
            = new(AuthenticationSteps.GivenOptions(false, ["apiGlobal"], [JwtBearerDefaults.AuthenticationScheme]))
            {
                RouteKeys = ["R2"],
            };
        var body = Body();
        var downstreamUrls = ports1.Union(ports2).Union(ports3).Select(DownstreamUrl).ToArray();
        var responses = Enumerable.Repeat(body, downstreamUrls.Length).ToArray();
        string[] extraScopes = ["api", "apiGlobal", "Mr.Who"];
        GivenMultipleServiceInstancesAreRunning(downstreamUrls, responses);
        steps.GivenThereIsAConfiguration(configuration);
        steps.GivenOcelotIsRunning(WithDiscoveryAndJwtBearerAuthentication(steps));
        await steps.GivenThereIsExternalJwtSigningService(extraScopes, CancelMe);
        GivenIEnsureOcelotClient(steps);
        await steps.GivenIHaveAToken("Mr.Who", null, null, null, body);
        steps.GivenIHaveAddedATokenToMyRequest();
        await Task.WhenAll(
            WhenIGetUrlOnTheApiGatewayConcurrently("/route1/", 2));
        ThenAllStatusCodesShouldBe(HttpStatusCode.OK); // auth is switched off and the scope doesn't matter
        ThenAllResponseBodiesShouldBe(body);

        await steps.GivenIHaveAToken(globalOptions.AllowedScopes[0], null, null, null, body);
        steps.GivenIHaveAddedATokenToMyRequest();
        await Task.WhenAll(
            WhenIGetUrlOnTheApiGatewayConcurrently("/route2/", 2));
        ThenAllStatusCodesShouldBe(HttpStatusCode.OK); // global scope has been accepted
        ThenAllResponseBodiesShouldBe(body);

        await steps.GivenIHaveAToken("Mr.Who", null, null, null, body); // should be different scope of route #3 which is "invalid-scope"
        steps.GivenIHaveAddedATokenToMyRequest();
        await Task.WhenAll(
            WhenIGetUrlOnTheApiGatewayConcurrently("/noAuthorization/", 2));
        ThenAllStatusCodesShouldBe(HttpStatusCode.Forbidden);
        ThenAllResponseBodiesShouldBe("0");
        ThenServicesShouldHaveBeenCalledTimes(1, 1, 1, 1, 0, 0);
    }

    [Fact]
    [Trait("Feat", "585")] // https://github.com/ThreeMammals/Ocelot/issues/585
    [Trait("Feat", "2338")] // https://github.com/ThreeMammals/Ocelot/issues/2338
    [Trait("PR", "2339")] // https://github.com/ThreeMammals/Ocelot/pull/2339
    public async Task ShouldApplyGlobalQosOptionsForAllDynamicRoutes()
    {
        var ports = PortFinder.GetPorts(3);
        var serviceUrls = ports.Select(DownstreamUrl).ToArray();
        var configuration = GivenDynamicRouting(new()
        {
            { ServiceName(), serviceUrls },
        });
        FileQoSOptions globalOptions = configuration.GlobalConfiguration.QoSOptions = new()
        {
            BreakDuration = 501, // CircuitBreakerStrategy.LowBreakDuration + 1
            MinimumThroughput = 2, // exceptions-errors
            Timeout = 500, // ms
        };
        using var steps = new QosSteps(this);
        Counters = new int[serviceUrls.Length];
        steps.CounterStrategy = (port) =>
        {
            int index = Array.FindIndex(serviceUrls, url => new Uri(url).Port == port);
            int count = Interlocked.Increment(ref Counters[index]);
        };
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithDiscoveryAndQualityOfService);
        await steps.TestRouteCircuitBreaker(ports, UpstreamPath(), globalOptions, 0, EnabledDiscovery); // test global scenario
        await steps.TestRouteTimeout(ports, UpstreamPath(), globalOptions);
        ThenServicesShouldHaveBeenCalledTimes(2, 2, 1);
    }

    [Fact]
    [Trait("Feat", "585")] // https://github.com/ThreeMammals/Ocelot/issues/585
    [Trait("Feat", "2338")] // https://github.com/ThreeMammals/Ocelot/issues/2338
    [Trait("PR", "2339")] // https://github.com/ThreeMammals/Ocelot/pull/2339
    public async Task ShouldApplyGlobalQosOptionsForAllDynamicRoutesWithGroupedOpts()
    {
        const int GlobalTimeout = 1500, GlobalExceptions = 3, GlobalBreakMs = 2000;
        var ports1 = PortFinder.GetPorts(2);

        // 1st route
        var route1 = GivenLbRoute("route1", key: null); // 1st route is not in the global group
        route1.QoSOptions = null; // 1st route has no opts
        GivenDiscoveryMetadata(route1, ports1);

        // 2nd route
        var ports2 = PortFinder.GetPorts(2);
        var route2 = GivenLbRoute("route2", key: "R2"); // 2nd route is in the group
        route2.QoSOptions = null; // 2nd route opts will be applied from global ones
        GivenDiscoveryMetadata(route2, ports2);

        // 3rd route
        var ports3 = PortFinder.GetPorts(2);
        var route3 = GivenLbRoute("noCircuitBreaker", loadBalancer: nameof(NoLoadBalancer), key: null);
        route3.QoSOptions = new()
        {
            MinimumThroughput = 0, // disable Circuit Breaker via disallowing of global opts to substitute
            BreakDuration = 0,
            Timeout = GlobalTimeout,
        };
        GivenDiscoveryMetadata(route3, ports3);

        var configuration = GivenDynamicRouting(new(), route1, route2, route3);
        var globalOptions = configuration.GlobalConfiguration.QoSOptions
            = new(new QoSOptions(GlobalExceptions, GlobalBreakMs))
            {
                RouteKeys = ["R2"],
            };
        var body = Body();
        var downstreamUrls = ports1.Union(ports2).Union(ports3).Select(DownstreamUrl).ToArray();
        using var steps = new QosSteps(this);
        steps.CounterStrategy = (port) =>
        {
            int index = Array.FindIndex(downstreamUrls, url => new Uri(url).Port == port);
            int count = Interlocked.Increment(ref Counters[index]);
        };
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithDiscoveryAndQualityOfService);
        GivenMultipleServiceInstancesAreRunning(downstreamUrls,
            Enumerable.Repeat(body, downstreamUrls.Length).ToArray(),
            Enumerable.Repeat(HttpStatusCode.NotFound, ports1.Length)
                .Concat(Enumerable.Repeat(HttpStatusCode.InternalServerError, ports2.Length))
                .Concat(Enumerable.Repeat(HttpStatusCode.OK, ports3.Length))
                .ToArray());
        await Task.WhenAll(
            WhenIGetUrlOnTheApiGatewayConcurrently($"/{route1.ServiceName}/", 2));
        ThenAllStatusCodesShouldBe(HttpStatusCode.NotFound); // QoS is switched off and the scope doesn't matter
        ThenAllResponseBodiesShouldBe(body);
        await steps.TestRouteCircuitBreaker(ports2, $"/{route2.ServiceName}/", globalOptions, 0, EnabledDiscovery); // test global scenario
        await steps.TestRouteTimeout(ports3, $"/{route3.ServiceName}/", route3.QoSOptions);
        ThenServicesShouldHaveBeenCalledTimes(1, 1, 3, 1, 2, 0);
    }

    [Fact]
    [Trait("Bug", "2352")] // https://github.com/ThreeMammals/Ocelot/issues/2352
    [Trait("PR", "2427")] // https://github.com/ThreeMammals/Ocelot/pull/2427
    public async Task ShouldRouteRequestWithChromiumClientHintHeaders()
    {
        var ports = PortFinder.GetPorts(1);
        var serviceName = ServiceName();
        var serviceUrls = ports.Select(DownstreamUrl).ToArray();
        var configuration = GivenDynamicRouting(new()
        {
            { serviceName, serviceUrls },
        });
        GivenMultipleServiceInstancesAreRunning(serviceUrls, serviceName);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithDiscovery);
        GivenIAddAHeader("sec-ch-ua", "\"Microsoft Edge\";v=\"143\", \"Chromium\";v=\"143\", \"Not A(Brand\";v=\"24\"");
        GivenIAddAHeader("sec-ch-ua-mobile", "?0");
        await WhenIGetUrlOnTheApiGateway(UpstreamPath());
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        ThenAllServicesShouldHaveBeenCalledTimes(1);
    }

    [Fact]
    [Trait("Bug", "2428")] // https://github.com/ThreeMammals/Ocelot/issues/2428
    [Trait("PR", "2429")] // https://github.com/ThreeMammals/Ocelot/pull/2429
    public async Task ShouldForwardEachRequestToItsOwnDownstreamPath()
    {
        var ports = PortFinder.GetPorts(1);
        var serviceUrls = ports.Select(DownstreamUrl).ToArray();
        var configuration = GivenDynamicRouting(new()
        {
            { ServiceName(), serviceUrls },
        });
        GivenMultipleServiceInstancesAreRunning(serviceUrls);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithDiscovery);
        await WhenIGetUrlOnTheApiGateway(UpstreamPath("/first"));
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        ThenTheResponseHeaderIs(HeaderNames.Path, "/first");
        await WhenIGetUrlOnTheApiGateway(UpstreamPath("/second?id=2"));
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        ThenTheResponseHeaderIs(HeaderNames.Path, "/second?id=2");
        await WhenIPostUrlOnTheApiGateway(UpstreamPath("/orders?flag&q=a+b"), "{}", MediaTypeNames.Application.Json);
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        ThenTheResponseHeaderIs(HeaderNames.Path, "/orders?flag&q=a+b");
        await WhenIDeleteUrlOnTheApiGateway(UpstreamPath("/orders/7?force=true"));
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        ThenTheResponseHeaderIs(HeaderNames.Path, "/orders/7?force=true");
        ThenAllServicesShouldHaveBeenCalledTimes(4);
    }

    [Fact]
    [Trait("Bug", "2428")] // https://github.com/ThreeMammals/Ocelot/issues/2428
    [Trait("PR", "2429")] // https://github.com/ThreeMammals/Ocelot/pull/2429
    public async Task ShouldUseOneHttpHandlerForAllDownstreamPathsOfService()
    {
        var ports = PortFinder.GetPorts(1);
        var serviceUrls = ports.Select(DownstreamUrl).ToArray();
        var configuration = GivenDynamicRouting(new()
        {
            { ServiceName(), serviceUrls },
        });
        var paths = Enumerable.Range(1, 10).Select(i => $"/users/{i}").ToArray();
        var urls = paths.Select(path => UpstreamPath(path)).ToArray();
        GivenMultipleServiceInstancesAreRunning(serviceUrls);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithDiscoveryAndRequesterTesting);
        await Task.WhenAll(
            WhenIGetUrlOnTheApiGatewayConcurrently(urls.Length, urls)); // TODO As the first cold request, it sends 10 requests to a SD provider to return a service instance.
        ThenAllStatusCodesShouldBe(HttpStatusCode.OK);
        ThenEachResponsePathShouldBe(paths);
        ThenOneHttpHandlerShouldBeCreated();
    }

    private FileDynamicRoute GivenLbRoute(string serviceName, string serviceNamespace = null,
        string loadBalancer = null, string key = null) => new()
        {
            ServiceName = serviceName,
            ServiceNamespace = serviceNamespace ?? ServiceNamespace(),
            LoadBalancerOptions = new(loadBalancer ?? nameof(RoundRobin)),
            Key = key,
        };

    private static void WithDiscoveryAndQualityOfService(IServiceCollection services) => services
            .AddSingleton(DynamicRoutingDiscoveryFinder)
            .AddOcelot().AddQualityOfService(); // Built-in feat, not Polly

    private void WithDiscoveryAndRequesterTesting(IServiceCollection services)
    {
        WithDiscovery(services);
        RequesterSteps.WithRequesterTesting(services, false);
    }
    private Action<IServiceCollection> WithDiscoveryAndJwtBearerAuthentication(AuthenticationSteps steps)
    {
        Action<IServiceCollection> ocelotServices = WithDiscovery;
        void withJwtBearerAuthentication(IServiceCollection services)
            => steps.WithJwtBearerAuthentication(services, false);
        ocelotServices += withJwtBearerAuthentication;
        return ocelotServices;
    }

    private void ThenRouteHttpHandlerOptionsAre(string serviceName, IDictionary<string, string> metadata,
        int maxConnections, int seconds, bool useTracing)
    {
        var pool = OcelotServices.GetService<IMessageInvokerPool>() as TestMessageInvokerPool;
        pool.ShouldNotBeNull();
        var tracer = OcelotServices.GetService<IOcelotTracer>() as TestOcelotTracer;
        tracer.ShouldNotBeNull();
        foreach (var kv in pool.CreatedHandlers.Where(x => x.Key.ServiceName == serviceName))
        {
            var downstream = kv.Key;
            var httpHandler = kv.Value;
            httpHandler.MaxConnectionsPerServer.ShouldBe(maxConnections);
            httpHandler.PooledConnectionLifetime.TotalSeconds.ShouldBe(seconds);
            downstream.HttpHandlerOptions.UseTracing.ShouldBe(useTracing);
        }
        var csvData = metadata[serviceName];
        var serviceUrls = csvData.Split(',');
        tracer.Requests.Count.ShouldBe(serviceUrls.Length);
        foreach (var url in serviceUrls)
        {
            var request = tracer.Requests.Keys.SingleOrDefault(k => k.RequestUri.AbsoluteUri.StartsWith(url));
            (request is not null).ShouldBe(useTracing);
        }
    }

    private void ThenEachResponsePathShouldBe(string[] paths)
    {
        foreach (var (index, response) in Responses)
        {
            response.ShouldNotBeNull().Headers.GetValues(HeaderNames.Path).ShouldHaveSingleItem()
                .ShouldBe(paths[index], "Should forward the request to its own downstream path");
        }
    }

    private void ThenOneHttpHandlerShouldBeCreated([CallerMemberName] string serviceName = null)
    {
        var pool = OcelotServices.GetService<IMessageInvokerPool>() as TestMessageInvokerPool;
        pool.ShouldNotBeNull().CreatedHandlers.Keys.Count(route => route.ServiceName == serviceName)
            .ShouldBe(1, "Should use one HTTP handler for all downstream paths of the service");
    }

    public override string ServiceNamespace() => nameof(DynamicRoutingTests);
    public override CancellationToken CancelMe => TestContext.Current.CancellationToken;
}
