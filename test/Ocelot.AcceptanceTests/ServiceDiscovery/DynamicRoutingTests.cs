using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.AcceptanceTests.Caching;
using Ocelot.AcceptanceTests.QualityOfService;
using Ocelot.AcceptanceTests.Requester;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.LoadBalancer.Balancers;
using Ocelot.Logging;
using Ocelot.Requester;
using Ocelot.Testing.Authentication;
using Ocelot.Testing.Steps;

namespace Ocelot.AcceptanceTests.ServiceDiscovery;

/// <summary>
/// These tests are based on the custom service discovery provider, abstracting from currently implemented discovery providers and focusing on the dynamic routing features.
/// </summary>
public class DynamicRoutingTests : DiscoverySteps
{
    public const bool EnabledDiscovery = true;

    [BddfyFact]
    [Trait("Feat", "351")] // https://github.com/ThreeMammals/Ocelot/pull/351
    public void ShouldForwardQueryStringToDownstream()
    {
        var ports = PortFinder.GetPorts(2);
        var serviceName = ServiceName();
        var serviceUrls = ports.Select(DownstreamUrl).ToArray();
        var configuration = GivenDynamicRouting(new()
        {
            { serviceName, serviceUrls },
        });
        var pathWithQueryString = $"/{serviceName}/?{nameof(TestID)}={TestID}";
        this
            .Given(x => GivenMultipleServiceInstancesAreRunning(serviceUrls, serviceName))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(WithDiscovery))
            .When(x => WhenIGetUrlOnTheApiGatewayConcurrently(pathWithQueryString, 2))
            .Then(x => ThenAllServicesShouldHaveBeenCalledTimes(2))
            .And(x => ThenServicesShouldHaveBeenCalledTimes(1, 1))
            .And(x => ThenAllResponsesHeaderExists(HeaderNames.Path))
            .And(x => ThenAllResponsesPathAndQueryShouldAllBeContainedInThePath(pathWithQueryString))
        .BDDfy();
    }
    private void ThenAllResponsesPathAndQueryShouldAllBeContainedInThePath(string pathWithQueryString)
    {
        var pathAndQuery = ThenAllResponsesHeaderExists(HeaderNames.Path).ToList();
        pathAndQuery.ShouldAllBe(pathQuery => pathWithQueryString.Contains(pathQuery));
    }

    [BddfyFact]
    [Trait("Feat", "585")] // https://github.com/ThreeMammals/Ocelot/issues/585
    [Trait("Feat", "2319")] // https://github.com/ThreeMammals/Ocelot/issues/2319
    [Trait("PR", "2324")] // https://github.com/ThreeMammals/Ocelot/pull/2324
    public void ShouldApplyGlobalLoadBalancerOptionsForAllDynamicRoutes()
    {
        var ports = PortFinder.GetPorts(5);
        var serviceName = ServiceName();
        var serviceUrls = ports.Select(DownstreamUrl).ToArray();
        var configuration = GivenDynamicRouting(new()
        {
            { serviceName, serviceUrls },
        });
        this
            .Given(x => GivenMultipleServiceInstancesAreRunning(serviceUrls, serviceName))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(WithDiscovery))
            .When(x => WhenIGetUrlOnTheApiGatewayConcurrently($"/{serviceName}/", 50))
            .Then(x => ThenAllServicesShouldHaveBeenCalledTimes(50))
            .And(x => ThenAllServicesCalledRealisticAmountOfTimes(9, 11)) // soft assertion
            .And(x => ThenServicesShouldHaveBeenCalledTimes(10, 10, 10, 10, 10)) // distribution by RoundRobin algorithm, aka strict assertion
        .BDDfy();
    }

    [BddfyFact]
    [Trait("Feat", "585")] // https://github.com/ThreeMammals/Ocelot/issues/585
    [Trait("Feat", "2319")] // https://github.com/ThreeMammals/Ocelot/issues/2319
    [Trait("PR", "2324")] // https://github.com/ThreeMammals/Ocelot/pull/2324
    public void ShouldApplyGlobalGroupLoadBalancerOptionsForDynamicRoutesWhenRouteOptsHasAKey()
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
        this
            .Given(x => GivenMultipleServiceInstancesAreRunning(downstreamUrls, serviceName))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(WithDiscovery))
            .When(x => WhenIGetUrlOnTheApiGatewayConcurrently("/route1/", 2))
            .And(x => WhenIGetUrlOnTheApiGatewayConcurrently("/route2/", 4))
            .And(x => WhenIGetUrlOnTheApiGatewayConcurrently("/noLoadBalancing/", 5))
            .Then(x => ThenServicesShouldHaveBeenCalledTimes(2, 0, 2, 2, 5, 0)) // main assertion, explanation is below
            .And(x => ThenServiceShouldHaveBeenCalledTimes(0, 2)) // NoLoadBalancer for 2
            .And(x => ThenServiceShouldHaveBeenCalledTimes(1, 0)) // NoLoadBalancer for 2
            .And(x => ThenServiceShouldHaveBeenCalledTimes(2, 2)) // RoundRobin for 4
            .And(x => ThenServiceShouldHaveBeenCalledTimes(3, 2)) // RoundRobin for 4
            .And(x => ThenServiceShouldHaveBeenCalledTimes(4, 5)) // NoLoadBalancer for 5
            .And(x => ThenServiceShouldHaveBeenCalledTimes(5, 0)) // NoLoadBalancer for 5
        .BDDfy();
    }

    [BddfyFact]
    [Trait("Feat", "585")] // https://github.com/ThreeMammals/Ocelot/issues/585
    [Trait("Feat", "2330")] // https://github.com/ThreeMammals/Ocelot/issues/2330
    [Trait("PR", "2331")] // https://github.com/ThreeMammals/Ocelot/pull/2331
    public void ShouldApplyGlobalCacheOptionsForAllDynamicRoutes()
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
        var scenario = this
            .Given(x => GivenMultipleServiceInstancesAreRunning(serviceUrls, responses))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(WithDiscovery));
            AssertCachedRoute(scenario, TTL, serviceName, ports, [testBody1, testBody2])
        .BDDfy();
    }

    private IFluentStepBuilder<DynamicRoutingTests> AssertCachedRoute(IFluentStepBuilder<DynamicRoutingTests> scenario,
        int ttl, string serviceName, int[] ports, string[] expectedBody, bool cached = true, bool balanced = true, int shift = 0)
    {
        var url = $"/{serviceName}/";
        int counter = cached ? 1 : 2;
        Action<int> GivenCounterIs = v => counter = v;
        scenario.Given(x => Array.Clear(Counters));
        scenario.When(x => WhenIGetUrlOnTheApiGatewayConcurrently(url, 2));
        scenario.Then(x => ThenAllServicesShouldHaveBeenCalledTimes(2));

        //ThenServicesShouldHaveBeenCalledTimes(1, 1); // distribution by RoundRobin algorithm, aka strict assertion
        scenario.And(x => ThenServiceShouldHaveBeenCalledTimes(shift + 0, balanced ? 1 : 2));
        scenario.And(x => ThenServiceShouldHaveBeenCalledTimes(shift + 1, balanced ? 1 : 0));

        scenario.Given(x => GivenIWaitAsync(100));
        scenario.When(x => WhenIGetUrlOnTheApiGatewayConcurrently(url, 2));
        scenario.Then(x => ThenAllServicesShouldHaveBeenCalledTimes(cached ? 2 : 4)); // the counters remain unchanged, and the items are still in the cache

        //ThenServicesShouldHaveBeenCalledTimes(counter, counter); // the counters remain unchanged
        scenario.And(x => ThenServiceShouldHaveBeenCalledTimes(shift + 0, balanced ? counter : 2 * counter));
        scenario.And(x => ThenServiceShouldHaveBeenCalledTimes(shift + 1, balanced ? counter : 0));

        scenario.Given(x => GivenIWaitAsync(ttl * 1000)); // allow cached items to expire
        scenario.When(x => WhenIGetUrlOnTheApiGatewayConcurrently(url, 2));
        scenario.Then(x => ThenAllServicesShouldHaveBeenCalledTimes(cached ? 4 : 6)); // the counters have been updated because new items were added to the cache
        scenario.Given(x => GivenCounterIs.Invoke(cached ? 2 : 3));

        //ThenServicesShouldHaveBeenCalledTimes(counter, counter); // the counters have been updated
        scenario.Then(x => ThenServiceShouldHaveBeenCalledTimes(shift + 0, balanced ? counter : 2 * counter));
        scenario.And(x => ThenServiceShouldHaveBeenCalledTimes(shift + 1, balanced ? counter : 0));
        scenario.And(x => ThenAllResponseBodiesShouldBe(ports, expectedBody));
        return scenario;
    }

    [BddfyFact]
    [Trait("Feat", "585")] // https://github.com/ThreeMammals/Ocelot/issues/585
    [Trait("Feat", "2330")] // https://github.com/ThreeMammals/Ocelot/issues/2330
    [Trait("PR", "2331")] // https://github.com/ThreeMammals/Ocelot/pull/2331
    public void ShouldApplyGlobalGroupCacheOptionsWhenRouteOptsHasAKey()
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
        string[] responses = [testBody1, testBody2, testBody1, testBody2, testBody1, testBody2];
        var scenario = this
            .Given(x => GivenMultipleServiceInstancesAreRunning(downstreamUrls, responses))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(WithDiscovery));
            AssertCachedRoute(scenario, TTL, route1.ServiceName, ports1, [testBody1, testBody2], cached: false, shift: 0)
            .Then(x => ThenICopyCounters1());
            AssertCachedRoute(scenario, TTL, route2.ServiceName, ports2, [testBody1, testBody2], cached: true, shift: 2)
            .Then(x => ThenICopyCounters2());
            AssertCachedRoute(scenario, TTL, route3.ServiceName, ports3, [testBody1, testBody2], cached: false, balanced: false, shift: 4)
            .Then(x => ThenICopyCounters3())
            .Then(x => SumCountersPerIndex())
            .And(x => ThenServicesShouldHaveBeenCalledTimes(3, 3, 2, 2, 6, 0)) // main assertion, explanation is below
            .And(x => ThenServiceShouldHaveBeenCalledTimes(0, 3)) // RoundRobin for 6, not cached
            .And(x => ThenServiceShouldHaveBeenCalledTimes(1, 3)) // RoundRobin for 6, not cached
            .And(x => ThenServiceShouldHaveBeenCalledTimes(2, 2)) // RoundRobin for 6, cached 1
            .And(x => ThenServiceShouldHaveBeenCalledTimes(3, 2)) // RoundRobin for 6, cached 1
            .And(x => ThenServiceShouldHaveBeenCalledTimes(4, 6)) // NoLoadBalancer for 6, not cached
            .And(x => ThenServiceShouldHaveBeenCalledTimes(5, 0)) // NoLoadBalancer for 6, not cached
        .BDDfy();
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

    [BddfyFact]
    [Trait("Feat", "585")] // https://github.com/ThreeMammals/Ocelot/issues/585
    [Trait("Feat", "2320")] // https://github.com/ThreeMammals/Ocelot/issues/2320
    [Trait("PR", "2332")] // https://github.com/ThreeMammals/Ocelot/pull/2332
    public void ShouldApplyGlobalHttpHandlerOptionsForAllDynamicRoutes()
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
        this
            .Given(x => GivenMultipleServiceInstancesAreRunning(serviceUrls, serviceName))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(WithDiscoveryAndRequesterTesting))
            .When(x => WhenIGetUrlOnTheApiGatewayConcurrently($"/{serviceName}/", times))
            .Then(x => ThenAllServicesShouldHaveBeenCalledTimes(times))
            .And(x => ThenServicesShouldHaveBeenCalledTimes(1, 1, 1)) // distribution by RoundRobin algorithm, aka strict assertion
            .And(x => ThenRouteHttpHandlerOptionsAre(serviceName, configuration.GlobalConfiguration.Metadata, 77, 88, true))
        .BDDfy();
    }

    [BddfyFact]
    [Trait("Feat", "585")] // https://github.com/ThreeMammals/Ocelot/issues/585
    [Trait("Feat", "2320")] // https://github.com/ThreeMammals/Ocelot/issues/2320
    [Trait("PR", "2332")] // https://github.com/ThreeMammals/Ocelot/pull/2332
    public void ShouldApplyGlobalGroupHttpHandlerOptionsForDynamicRoutesWhenRouteOptsHasAKey()
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
        this
            .Given(x => GivenMultipleServiceInstancesAreRunning(downstreamUrls, serviceName))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(WithDiscoveryAndRequesterTesting))
            .When(x => WhenIGetUrlOnTheApiGatewayConcurrently("/route1/", 2))
            .And(x => WhenIGetUrlOnTheApiGatewayConcurrently("/route2/", 2))
            .And(x => WhenIGetUrlOnTheApiGatewayConcurrently("/noTracing/", 2))
            .Then(x => ThenServicesShouldHaveBeenCalledTimes(1, 1, 1, 1, 2, 0))
            .And(x => ThenRouteHttpHandlerOptionsAre(route1.ServiceName, route1.Metadata,
                int.MaxValue, HttpHandlerOptions.DefaultPooledConnectionLifetimeSeconds, false)) // default opts
            .And(x => ThenRouteHttpHandlerOptionsAre(route2.ServiceName, route2.Metadata,
                globalOpts.MaxConnectionsPerServer.Value, globalOpts.PooledConnectionLifetimeSeconds.Value, globalOpts.UseTracing.Value)) // global opts
            .And(x => ThenRouteHttpHandlerOptionsAre(route3.ServiceName, route3.Metadata,
                route3Opts.MaxConnectionsPerServer.Value, route3Opts.PooledConnectionLifetimeSeconds.Value, route3Opts.UseTracing.Value)) // route opts
        .BDDfy();
    }

    [BddfyFact]
    [Trait("Feat", "585")] // https://github.com/ThreeMammals/Ocelot/issues/585
    [Trait("Feat", "2316")] // https://github.com/ThreeMammals/Ocelot/issues/2316
    [Trait("PR", "2336")] // https://github.com/ThreeMammals/Ocelot/pull/2336
    public void ShouldApplyGlobalAuthenticationOptionsForAllDynamicRoutes()
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
        this
            .Given(x => GivenMultipleServiceInstancesAreRunning(serviceUrls, responses))
            .And(x => steps.GivenThereIsAConfiguration(configuration))
            .And(x => steps.GivenOcelotIsRunning(WithDiscoveryAndJwtBearerAuthentication(steps)))
            .And(x => steps.GivenThereIsExternalJwtSigningService(scopes, CancelMe))
            .And(x => steps.GivenIHaveAToken(scopes[0], null, null, null, serviceName)) //,audience: ocelotClient.BaseAddress.Authority);
            .And(x => steps.GivenIHaveAddedATokenToMyRequest())
            .And(x => GivenIEnsureOcelotClient(steps))
            .When(x => WhenIGetUrlOnTheApiGatewayConcurrently($"/{serviceName}/", times))
            .Then(x => ThenAllServicesShouldHaveBeenCalledTimes(times))
            .And(x => ThenServicesShouldHaveBeenCalledTimes(1, 1, 1)) // distribution by RoundRobin algorithm, aka strict assertion
            .And(x => ThenAllStatusCodesShouldBe(HttpStatusCode.OK))
            .And(x => ThenAllResponseBodiesShouldBe(serviceName))
        .BDDfy();
    }
    private void GivenIEnsureOcelotClient(AuthenticationSteps steps)
        => ocelotClient ??= steps.OcelotClient;

    [BddfyFact]
    [Trait("Feat", "585")] // https://github.com/ThreeMammals/Ocelot/issues/585
    [Trait("Feat", "2316")] // https://github.com/ThreeMammals/Ocelot/issues/2316
    [Trait("PR", "2336")] // https://github.com/ThreeMammals/Ocelot/pull/2336
    public void ShouldApplyGlobalGroupAuthenticationOptionsForDynamicRoutesWhenRouteOptsHasAKey()
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
        this
            .Given(x => GivenMultipleServiceInstancesAreRunning(downstreamUrls, responses))
            .And(x => steps.GivenThereIsAConfiguration(configuration))
            .And(x => steps.GivenOcelotIsRunning(WithDiscoveryAndJwtBearerAuthentication(steps)))
            .And(x => steps.GivenThereIsExternalJwtSigningService(extraScopes, CancelMe))
            .And(x => GivenIEnsureOcelotClient(steps))
            .And(x => steps.GivenIHaveAToken("Mr.Who", null, null, null, body))
            .And(x => steps.GivenIHaveAddedATokenToMyRequest())
            .When(x => WhenIGetUrlOnTheApiGatewayConcurrently("/route1/", 2))
            .Then(x => ThenAllStatusCodesShouldBe(HttpStatusCode.OK)) // auth is switched off and the scope doesn't matter
            .And(x => ThenAllResponseBodiesShouldBe(body))

            .Given(x => steps.GivenIHaveAToken(globalOptions.AllowedScopes[0], null, null, null, body))
            .And(x => steps.GivenIHaveAddedATokenToMyRequest())
            .When(x => WhenIGetUrlOnTheApiGatewayConcurrently("/route2/", 2))
            .Then(x => ThenAllStatusCodesShouldBe(HttpStatusCode.OK)) // global scope has been accepted
            .And(x => ThenAllResponseBodiesShouldBe(body))

            .And(x => steps.GivenIHaveAToken("Mr.Who", null, null, null, body)) // should be different scope of route #3 which is "invalid-scope"
            .And(x => steps.GivenIHaveAddedATokenToMyRequest())
            .When(x => WhenIGetUrlOnTheApiGatewayConcurrently("/noAuthorization/", 2))
            .Then(x => ThenAllStatusCodesShouldBe(HttpStatusCode.Forbidden))
            .And(x => ThenAllResponseBodiesShouldBe("0"))
            .And(x => ThenServicesShouldHaveBeenCalledTimes(1, 1, 1, 1, 0, 0))
        .BDDfy();
    }

    [BddfyFact]
    [Trait("Feat", "585")] // https://github.com/ThreeMammals/Ocelot/issues/585
    [Trait("Feat", "2338")] // https://github.com/ThreeMammals/Ocelot/issues/2338
    [Trait("PR", "2339")] // https://github.com/ThreeMammals/Ocelot/pull/2339
    public void ShouldApplyGlobalQosOptionsForAllDynamicRoutes()
    {
        var ports = PortFinder.GetPorts(3);
        var serviceName = ServiceName();
        var serviceUrls = ports.Select(DownstreamUrl).ToArray();
        var configuration = GivenDynamicRouting(new()
        {
            { serviceName, serviceUrls },
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
        this
            .Given(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(WithDiscoveryAndQualityOfService))
            .When(x => steps.TestRouteCircuitBreaker(ports, $"/{serviceName}/", globalOptions, 0, EnabledDiscovery)) // test global scenario
            .And(x => steps.TestRouteTimeout(ports, $"/{serviceName}/", globalOptions))
            .Then(x => ThenServicesShouldHaveBeenCalledTimes(2, 2, 1))
        .BDDfy();
    }

    [BddfyFact]
    [Trait("Feat", "585")] // https://github.com/ThreeMammals/Ocelot/issues/585
    [Trait("Feat", "2338")] // https://github.com/ThreeMammals/Ocelot/issues/2338
    [Trait("PR", "2339")] // https://github.com/ThreeMammals/Ocelot/pull/2339
    public void ShouldApplyGlobalQosOptionsForAllDynamicRoutesWithGroupedOpts()
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
        this.Given(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(WithDiscoveryAndQualityOfService))
            .And(x => GivenMultipleServiceInstancesAreRunning(downstreamUrls,
                Enumerable.Repeat(body, downstreamUrls.Length).ToArray(),
                Enumerable.Repeat(HttpStatusCode.NotFound, ports1.Length)
                    .Concat(Enumerable.Repeat(HttpStatusCode.InternalServerError, ports2.Length))
                    .Concat(Enumerable.Repeat(HttpStatusCode.OK, ports3.Length))
                    .ToArray()))
            .When(x => WhenIGetUrlOnTheApiGatewayConcurrently($"/{route1.ServiceName}/", 2))
            .Then(x => ThenAllStatusCodesShouldBe(HttpStatusCode.NotFound)) // QoS is switched off and the scope doesn't matter
            .And(x => ThenAllResponseBodiesShouldBe(body))
            .When(x => steps.TestRouteCircuitBreaker(ports2, $"/{route2.ServiceName}/", globalOptions, 0, EnabledDiscovery)) // test global scenario
            .And(x => steps.TestRouteTimeout(ports3, $"/{route3.ServiceName}/", route3.QoSOptions))
            .Then(x => ThenServicesShouldHaveBeenCalledTimes(1, 1, 3, 1, 2, 0))
        .BDDfy();
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

    private static void WithDiscoveryAndRequesterTesting(IServiceCollection services)
    {
        WithDiscovery(services);
        RequesterSteps.WithRequesterTesting(services, false);
    }
    private static Action<IServiceCollection> WithDiscoveryAndJwtBearerAuthentication(AuthenticationSteps steps)
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
        var tracer = OcelotServices.GetService<IOcelotTracer>() as TestTracer;
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

    protected override string ServiceNamespace() => nameof(DynamicRoutingTests);
}
