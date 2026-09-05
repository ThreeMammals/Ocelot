using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.LoadBalancer.Balancers;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Testing.LoadBalancer;
using Ocelot.Testing.Steps;
using Ocelot.Values;

namespace Ocelot.Acceptance.LoadBalancer;

public sealed class LoadBalancerTests : ConcurrentSteps
{
    [Theory]
    [Trait("Feat", "211")] // https://github.com/ThreeMammals/Ocelot/pull/211
    [InlineData(false)] // original scenario, clean config
    [InlineData(true)] // extended scenario using analyzer
    public void ShouldLoadBalanceRequestWithLeastConnection(bool withAnalyzer)
    {
        var ports = PortFinder.GetPorts(2);
        var route = GivenLbRoute(ports, withAnalyzer ? nameof(LeastConnectionAnalyzer) : nameof(LeastConnection));
        var configuration = GivenConfiguration(route);
        var downstreamServiceUrls = ports.Select(DownstreamUrl).ToArray();
        LeastConnectionAnalyzer lbAnalyzer = null;
        LeastConnectionAnalyzer getAnalyzer(DownstreamRoute route, IServiceDiscoveryProvider provider)
        {
            //lock (LoadBalancerHouse.SyncRoot) // Note, synch locking is implemented in LoadBalancerHouse
            return lbAnalyzer ??= new LeastConnectionAnalyzerCreator().Create(route, provider)?.Data as LeastConnectionAnalyzer;
        }
        Action<IServiceCollection> withLeastConnectionAnalyzer = (s)
            => s.AddOcelot().AddCustomLoadBalancer<LeastConnectionAnalyzer>(getAnalyzer);
        var serviceName = TestName();
        this
            .Given(x => GivenMultipleServiceInstancesAreRunning(downstreamServiceUrls, serviceName))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(withAnalyzer ? withLeastConnectionAnalyzer : WithAddOcelot))
            .When(x => WhenIGetUrlOnTheApiGatewayConcurrently("/", 99))
            .Then(x => ThenAllServicesShouldHaveBeenCalledTimes(99))
            .And(x => ThenAllServicesCalledOptimisticAmountOfTimes(lbAnalyzer))
            .And(x => ThenServiceCountersShouldMatchLeasingCounters(lbAnalyzer, ports, 99))
            .And(x => ThenAllServicesCalledRealisticAmountOfTimes(
#if NET10_0_OR_GREATER
                Bottom(99, ports.Length) - 3, Top(99, ports.Length) + 3
#else
                Bottom(99, ports.Length), Top(99, ports.Length)
#endif
                ))
            // .And(x => ThenServicesShouldHaveBeenCalledTimes(50, 49)) // strict assertion, this is ideal case when load is not high
            .And(x => Counters.ShouldAllBe(c =>
#if NET10_0_OR_GREATER
                c <= 53 && c >= 46,
#else
                c == 50 || c == 49,
#endif
                CalledTimesMessage())) // LeastConnection algorithm distributes counters as 49/50 or 50/49 depending on thread synchronization
        .BDDfy();
    }

    [Theory]
    [Trait("Bug", "365")] // https://github.com/ThreeMammals/Ocelot/pull/365
    [InlineData(false)] // original scenario, clean config
    [InlineData(true)] // extended scenario using analyzer
    public void ShouldLoadBalanceRequestWithRoundRobin(bool withAnalyzer)
    {
        var ports = PortFinder.GetPorts(2);
        var route = GivenLbRoute(ports, withAnalyzer ? nameof(RoundRobinAnalyzer) : nameof(RoundRobin));
        var configuration = GivenConfiguration(route);
        var downstreamServiceUrls = ports.Select(DownstreamUrl).ToArray();
        RoundRobinAnalyzer lbAnalyzer = null;
        RoundRobinAnalyzer getAnalyzer(DownstreamRoute route, IServiceDiscoveryProvider provider)
        {
            //lock (LoadBalancerHouse.SyncRoot) // Note, synch locking is implemented in LoadBalancerHouse
            return lbAnalyzer ??= new RoundRobinAnalyzerCreator().Create(route, provider)?.Data as RoundRobinAnalyzer;
        }
        Action<IServiceCollection> withRoundRobinAnalyzer = (s)
            => s.AddOcelot().AddCustomLoadBalancer<RoundRobinAnalyzer>(getAnalyzer);
        var serviceName = TestName();
        this
            .Given(x => GivenMultipleServiceInstancesAreRunning(downstreamServiceUrls, serviceName))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(withAnalyzer ? withRoundRobinAnalyzer : WithAddOcelot))
            .When(x => WhenIGetUrlOnTheApiGatewayConcurrently("/", 99))
            .Then(x => ThenAllServicesShouldHaveBeenCalledTimes(99))
            .And(x => ThenAllServicesCalledOptimisticAmountOfTimes(lbAnalyzer))
            .And(x => ThenServiceCountersShouldMatchLeasingCounters(lbAnalyzer, ports, 99))
            .And(x => ThenAllServicesCalledRealisticAmountOfTimes(Bottom(99, ports.Length), Top(99, ports.Length)))
            .And(x => ThenServicesShouldHaveBeenCalledTimes(50, 49)) // strict assertion
        .BDDfy();
    }

    [Fact]
    [Trait("Feat", "961")] // https://github.com/ThreeMammals/Ocelot/issues/961
    public void ShouldLoadBalanceRequestWithCustomLoadBalancer()
    {
        static CustomLoadBalancer GetLoadBalancer(IServiceProvider serviceProvider, DownstreamRoute route, IServiceDiscoveryProvider discoveryProvider)
            => new(discoveryProvider.GetAsync);
        var ports = PortFinder.GetPorts(2);
        var route = GivenLbRoute(ports, nameof(CustomLoadBalancer));
        var configuration = GivenConfiguration(route);
        var downstreamServiceUrls = ports.Select(DownstreamUrl).ToArray();
        Action<IServiceCollection> withCustomLoadBalancer = (s)
            => s.AddOcelot().AddCustomLoadBalancer<CustomLoadBalancer>(GetLoadBalancer);
        var serviceName = TestName();
        this
            .Given(x => GivenMultipleServiceInstancesAreRunning(downstreamServiceUrls, serviceName))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(withCustomLoadBalancer))
            .When(x => WhenIGetUrlOnTheApiGatewayConcurrently("/", 50))
            .Then(x => ThenAllServicesShouldHaveBeenCalledTimes(50))
            .And(x => ThenAllServicesCalledRealisticAmountOfTimes(Bottom(50, ports.Length), Top(50, ports.Length)))
            .And(x => ThenServicesShouldHaveBeenCalledTimes(25, 25)) // strict assertion
        .BDDfy();
    }

    [Fact]
    [Trait("Feat", "585")] // https://github.com/ThreeMammals/Ocelot/issues/585
    [Trait("Feat", "2319")] // https://github.com/ThreeMammals/Ocelot/issues/2319
    [Trait("PR", "2324")] // https://github.com/ThreeMammals/Ocelot/pull/2324
    public void ShouldApplyGlobalOptions_ForStaticRoutes()
    {
        var ports1 = PortFinder.GetPorts(2);
        var route1 = GivenLbRoute(ports1, upstream: "/route1");
        route1.LoadBalancerOptions = new(); // no load balancing -> use global opts
        var ports2 = PortFinder.GetPorts(2);
        var route2 = GivenLbRoute(ports2, nameof(LeastConnection), "/route2");
        var ports3 = PortFinder.GetPorts(2);
        var route3 = GivenLbRoute(ports3, nameof(NoLoadBalancer), "/noLoadBalancing");

        var configuration = GivenConfiguration(route1, route2, route3); // static routes come to Routes collection
        configuration.GlobalConfiguration.LoadBalancerOptions = new(nameof(RoundRobin));

        var downstreamUrls = ports1.Union(ports2).Union(ports3).Select(DownstreamUrl).ToArray();
        var serviceName = TestName();
        this
            .Given(x => GivenMultipleServiceInstancesAreRunning(downstreamUrls, serviceName))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGatewayConcurrently("/route1", 2))
            .And(x => WhenIGetUrlOnTheApiGatewayConcurrently("/route2", 5))
            .And(x => WhenIGetUrlOnTheApiGatewayConcurrently("/noLoadBalancing", 7))
            .Then(x => ThenServicesShouldHaveBeenCalledTimes(1, 1, 3, 2, 7, 0)) // main assertion, explanation is below
            .And(x => ThenServiceShouldHaveBeenCalledTimes(0, 1)) // RoundRobin for 2
            .And(x => ThenServiceShouldHaveBeenCalledTimes(1, 1)) // RoundRobin for 2
            .And(x => ThenServiceShouldHaveBeenCalledTimes(2, 3)) // LeastConnection for 5
            .And(x => ThenServiceShouldHaveBeenCalledTimes(3, 2)) // LeastConnection for 5
            .And(x => ThenServiceShouldHaveBeenCalledTimes(4, 7)) // NoLoadBalancer for 7
            .And(x => ThenServiceShouldHaveBeenCalledTimes(5, 0)) // NoLoadBalancer for 7
        .BDDfy();
    }

    [Fact]
    [Trait("Feat", "585")] // https://github.com/ThreeMammals/Ocelot/issues/585
    [Trait("Feat", "2319")] // https://github.com/ThreeMammals/Ocelot/issues/2319
    [Trait("PR", "2324")] // https://github.com/ThreeMammals/Ocelot/pull/2324
    public void ShouldApplyGlobalGroupOptionsForStaticRoutesWhenRouteOptsHasAKey()
    {
        // 1st route
        var ports1 = PortFinder.GetPorts(2);
        var route1 = GivenLbRoute(ports1, upstream: "/route1");
        route1.LoadBalancerOptions = null; // 1st route is not balanced
        route1.Key = null; // 1st route is not in the global group

        // 2nd route
        var ports2 = PortFinder.GetPorts(2);
        var route2 = GivenLbRoute(ports2, upstream: "/route2");
        route2.LoadBalancerOptions = null; // 2nd route opts will be applied from global ones
        route2.Key = "R2"; // 2nd route is in the group

        // 3rd route
        var ports3 = PortFinder.GetPorts(2);
        var route3 = GivenLbRoute(ports3, nameof(NoLoadBalancer), "/noLoadBalancing");

        var configuration = GivenConfiguration(route1, route2, route3);
        configuration.GlobalConfiguration.LoadBalancerOptions = new()
        {
            RouteKeys = ["R2"],
            Type = nameof(RoundRobin),
        };

        var serviceName = TestName();
        var downstreamUrls = ports1.Union(ports2).Union(ports3).Select(DownstreamUrl).ToArray();
        this
            .Given(x => GivenMultipleServiceInstancesAreRunning(downstreamUrls, serviceName))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGatewayConcurrently("/route1", 2))
            .And(x => WhenIGetUrlOnTheApiGatewayConcurrently("/route2", 4))
            .And(x => WhenIGetUrlOnTheApiGatewayConcurrently("/noLoadBalancing", 5))
            .Then(x => ThenServicesShouldHaveBeenCalledTimes(2, 0, 2, 2, 5, 0)) // main assertion, explanation is below
            .And(x => ThenServiceShouldHaveBeenCalledTimes(0, 2)) // NoLoadBalancer for 2
            .And(x => ThenServiceShouldHaveBeenCalledTimes(1, 0)) // NoLoadBalancer for 2
            .And(x => ThenServiceShouldHaveBeenCalledTimes(2, 2)) // RoundRobin for 4
            .And(x => ThenServiceShouldHaveBeenCalledTimes(3, 2)) // RoundRobin for 4
            .And(x => ThenServiceShouldHaveBeenCalledTimes(4, 5)) // NoLoadBalancer for 5
            .And(x => ThenServiceShouldHaveBeenCalledTimes(5, 0)) // NoLoadBalancer for 5
        .BDDfy();
    }

    private sealed class CustomLoadBalancer : ILoadBalancer
    {
        private readonly Func<Task<List<Service>>> _services;
#if NET9_0_OR_GREATER
        private static readonly Lock _lock = new();
#else
        private static readonly object _lock = new();
#endif
        private int _last;

        public string Type => nameof(CustomLoadBalancer);
        public CustomLoadBalancer(Func<Task<List<Service>>> services) => _services = services;

        public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
        {
            var services = await _services();
            lock (_lock)
            {
                if (_last >= services.Count) _last = 0;
                var next = services[_last++];
                return new OkResponse<ServiceHostAndPort>(next.HostAndPort);
            }
        }

        public void Release(ServiceHostAndPort hostAndPort) { }
    }

    private FileRoute GivenLbRoute(int[] ports, string loadBalancer = null, string upstream = null)
    {
        var route = GivenRoute(ports[0], upstream: upstream);
        route.DownstreamHostAndPorts = ports.Select(Localhost).ToList();
        route.LoadBalancerOptions = new(loadBalancer ?? nameof(LeastConnection));
        return route;
    }
}
