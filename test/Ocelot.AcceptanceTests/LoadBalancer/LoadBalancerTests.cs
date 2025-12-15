using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.LoadBalancer.Balancers;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;

namespace Ocelot.AcceptanceTests.LoadBalancer;

public sealed class LoadBalancerTests : ConcurrentSteps
{
    [Theory]
    [Trait("Feat", "211")]
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
        GivenMultipleServiceInstancesAreRunning(downstreamServiceUrls);
        this.Given(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(withAnalyzer ? withLeastConnectionAnalyzer : WithAddOcelot))
            .When(x => WhenIGetUrlOnTheApiGatewayConcurrently("/", 99))
            .Then(x => ThenAllServicesShouldHaveBeenCalledTimes(99))
            .And(x => ThenAllServicesCalledOptimisticAmountOfTimes(lbAnalyzer))
            .And(x => ThenServiceCountersShouldMatchLeasingCounters(lbAnalyzer, ports, 99))
            .And(x => ThenAllServicesCalledRealisticAmountOfTimes(Bottom(99, ports.Length), Top(99, ports.Length)))

            // .And(x => ThenServicesShouldHaveBeenCalledTimes(50, 49)) // strict assertion, this is ideal case when load is not high
            .And(x => _counters.ShouldAllBe(c => c == 50 || c == 49, CalledTimesMessage())) // LeastConnection algorithm distributes counters as 49/50 or 50/49 depending on thread synchronization
            .BDDfy();
    }

    [Theory]
    [Trait("Bug", "365")]
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
        GivenMultipleServiceInstancesAreRunning(downstreamServiceUrls);
        this.Given(x => GivenThereIsAConfiguration(configuration))
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
    [Trait("Feat", "961")]
    public void ShouldLoadBalanceRequestWithCustomLoadBalancer()
    {
        Func<IServiceProvider, DownstreamRoute, IServiceDiscoveryProvider, CustomLoadBalancer> loadBalancerFactoryFunc =
            (serviceProvider, route, discoveryProvider) => new CustomLoadBalancer(discoveryProvider.GetAsync);
        var ports = PortFinder.GetPorts(2);
        var route = GivenLbRoute(ports, nameof(CustomLoadBalancer));
        var configuration = GivenConfiguration(route);
        var downstreamServiceUrls = ports.Select(DownstreamUrl).ToArray();
        Action<IServiceCollection> withCustomLoadBalancer = (s)
            => s.AddOcelot().AddCustomLoadBalancer<CustomLoadBalancer>(loadBalancerFactoryFunc);
        GivenMultipleServiceInstancesAreRunning(downstreamServiceUrls);
        this.Given(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(withCustomLoadBalancer))
            .When(x => WhenIGetUrlOnTheApiGatewayConcurrently("/", 50))
            .Then(x => ThenAllServicesShouldHaveBeenCalledTimes(50))
            .And(x => ThenAllServicesCalledRealisticAmountOfTimes(Bottom(50, ports.Length), Top(50, ports.Length)))
            .And(x => ThenServicesShouldHaveBeenCalledTimes(25, 25)) // strict assertion
            .BDDfy();
    }

    [Fact]
    [Trait("Feat", "585")]
    [Trait("Feat", "2319")]
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
        GivenMultipleServiceInstancesAreRunning(downstreamUrls);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();

        WhenIGetUrlOnTheApiGatewayConcurrently("/route1", 2);
        WhenIGetUrlOnTheApiGatewayConcurrently("/route2", 5);
        WhenIGetUrlOnTheApiGatewayConcurrently("/noLoadBalancing", 7);

        ThenServicesShouldHaveBeenCalledTimes(1, 1, 3, 2, 7, 0); // main assertion, explanation is below
        ThenServiceShouldHaveBeenCalledTimes(0, 1); // RoundRobin for 2
        ThenServiceShouldHaveBeenCalledTimes(1, 1); // RoundRobin for 2
        ThenServiceShouldHaveBeenCalledTimes(2, 3); // LeastConnection for 5
        ThenServiceShouldHaveBeenCalledTimes(3, 2); // LeastConnection for 5
        ThenServiceShouldHaveBeenCalledTimes(4, 7); // NoLoadBalancer for 7
        ThenServiceShouldHaveBeenCalledTimes(5, 0); // NoLoadBalancer for 7
    }

    [Fact]
    [Trait("Feat", "585")]
    [Trait("Feat", "2319")]
    [Trait("PR", "2324")] // https://github.com/ThreeMammals/Ocelot/pull/2324
    public void ShouldApplyGlobalGroupOptions_ForStaticRoutes_WhenRouteOptsHasAKey()
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

        var downstreamUrls = ports1.Union(ports2).Union(ports3).Select(DownstreamUrl).ToArray();
        GivenMultipleServiceInstancesAreRunning(downstreamUrls);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();

        WhenIGetUrlOnTheApiGatewayConcurrently("/route1", 2);
        WhenIGetUrlOnTheApiGatewayConcurrently("/route2", 4);
        WhenIGetUrlOnTheApiGatewayConcurrently("/noLoadBalancing", 5);
        ThenServicesShouldHaveBeenCalledTimes(2, 0, 2, 2, 5, 0); // main assertion, explanation is below
        ThenServiceShouldHaveBeenCalledTimes(0, 2); // NoLoadBalancer for 2
        ThenServiceShouldHaveBeenCalledTimes(1, 0); // NoLoadBalancer for 2
        ThenServiceShouldHaveBeenCalledTimes(2, 2); // RoundRobin for 4
        ThenServiceShouldHaveBeenCalledTimes(3, 2); // RoundRobin for 4
        ThenServiceShouldHaveBeenCalledTimes(4, 5); // NoLoadBalancer for 5
        ThenServiceShouldHaveBeenCalledTimes(5, 0); // NoLoadBalancer for 5
    }

    private sealed class CustomLoadBalancer : ILoadBalancer
    {
        private readonly Func<Task<List<Service>>> _services;
        private static readonly object _lock = new();
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
