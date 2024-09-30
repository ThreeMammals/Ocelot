using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;

namespace Ocelot.AcceptanceTests.LoadBalancer;

public sealed class LoadBalancerTests : ConcurrentSteps, IDisposable
{
    [Theory]
    [Trait("Feat", "211")]
    [InlineData(false)] // original scenario, clean config
    [InlineData(true)] // extended scenario using analyzer
    public void ShouldLoadBalanceRequestWithLeastConnection(bool withAnalyzer)
    {
        var ports = PortFinder.GetPorts(2);
        var route = GivenRoute(withAnalyzer ? nameof(LeastConnectionAnalyzer) : nameof(LeastConnection), ports);
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
            .And(x => GivenOcelotIsRunningWithServices(withAnalyzer ? withLeastConnectionAnalyzer : WithAddOcelot))
            .When(x => WhenIGetUrlOnTheApiGatewayConcurrently("/", 99))
            .Then(x => ThenAllServicesShouldHaveBeenCalledTimes(99))
            .And(x => ThenAllServicesCalledOptimisticAmountOfTimes(lbAnalyzer))
            .And(x => ThenServiceCountersShouldMatchLeasingCounters(lbAnalyzer, ports, 99))
            .And(x => ThenAllServicesCalledRealisticAmountOfTimes(Bottom(99, ports.Length), Top(99, ports.Length)))
            .And(x => ThenServicesShouldHaveBeenCalledTimes(50, 49)) // strict assertion
            .BDDfy();
    }

    [Theory]
    [Trait("Bug", "365")]
    [InlineData(false)] // original scenario, clean config
    [InlineData(true)] // extended scenario using analyzer
    public void ShouldLoadBalanceRequestWithRoundRobin(bool withAnalyzer)
    {
        var ports = PortFinder.GetPorts(2);
        var route = GivenRoute(withAnalyzer ? nameof(RoundRobinAnalyzer) : nameof(RoundRobin), ports);
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
            .And(x => GivenOcelotIsRunningWithServices(withAnalyzer ? withRoundRobinAnalyzer : WithAddOcelot))
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
        var route = GivenRoute(nameof(CustomLoadBalancer), ports);
        var configuration = GivenConfiguration(route);
        var downstreamServiceUrls = ports.Select(DownstreamUrl).ToArray();
        Action<IServiceCollection> withCustomLoadBalancer = (s)
            => s.AddOcelot().AddCustomLoadBalancer<CustomLoadBalancer>(loadBalancerFactoryFunc);
        GivenMultipleServiceInstancesAreRunning(downstreamServiceUrls);
        this.Given(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithServices(withCustomLoadBalancer))
            .When(x => WhenIGetUrlOnTheApiGatewayConcurrently("/", 50))
            .Then(x => ThenAllServicesShouldHaveBeenCalledTimes(50))
            .And(x => ThenAllServicesCalledRealisticAmountOfTimes(Bottom(50, ports.Length), Top(50, ports.Length)))
            .And(x => ThenServicesShouldHaveBeenCalledTimes(25, 25)) // strict assertion
            .BDDfy();
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

    private FileRoute GivenRoute(string loadBalancer, params int[] ports) => new()
    {
        DownstreamPathTemplate = "/",
        DownstreamScheme = Uri.UriSchemeHttp,
        UpstreamPathTemplate = "/",
        UpstreamHttpMethod = new() { HttpMethods.Get },
        LoadBalancerOptions = new() { Type = loadBalancer ?? nameof(LeastConnection) },
        DownstreamHostAndPorts = ports.Select(Localhost).ToList(),
    };
}
