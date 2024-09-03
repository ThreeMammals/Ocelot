using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;

namespace Ocelot.AcceptanceTests;

public sealed class LoadBalancerTests : ConcurrentSteps, IDisposable
{
    public LoadBalancerTests()
    {
    }

    public override void Dispose()
    {
        base.Dispose();
    }

    [Fact]
    [Trait("Feat", "211")]
    public void ShouldLoadBalanceRequest_WithLeastConnection()
    {
        var ports = PortFinder.GetPorts(2);
        var route = GivenRoute(nameof(LeastConnection), ports);
        var configuration = GivenConfiguration(route);
        var downstreamServiceUrls = ports.Select(DownstreamUrl).ToArray();
        GivenMultipleServiceInstancesAreRunning(downstreamServiceUrls);
        this.Given(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGatewayConcurrently("/", 50))
            .Then(x => x.ThenTheTwoServicesShouldHaveBeenCalledTimes(50))

            // Quite risky assertion because the actual values based on health checks and threading
            .And(x => x.ThenBothServicesCalledRealisticAmountOfTimes(1, 49)) // (24, 26)
            .BDDfy();
    }

    [Fact]
    [Trait("Bug", "365")]
    public void ShouldLoadBalanceRequest_WithRoundRobin()
    {
        var ports = PortFinder.GetPorts(2);
        var route = GivenRoute(nameof(RoundRobin), ports);
        var configuration = GivenConfiguration(route);
        var downstreamServiceUrls = ports.Select(DownstreamUrl).ToArray();
        GivenMultipleServiceInstancesAreRunning(downstreamServiceUrls);
        this.Given(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGatewayConcurrently("/", 50))
            .Then(x => x.ThenTheTwoServicesShouldHaveBeenCalledTimes(50))

            // Quite risky assertion because the actual values based on health checks and threading
            .And(x => x.ThenBothServicesCalledRealisticAmountOfTimes(1, 49)) // (24, 26)
            .BDDfy();
    }

    [Fact]
    [Trait("Feat", "961")]
    public void ShouldLoadBalanceRequest_WithCustomLoadBalancer()
    {
        Func<IServiceProvider, DownstreamRoute, IServiceDiscoveryProvider, CustomLoadBalancer> loadBalancerFactoryFunc =
            (serviceProvider, route, discoveryProvider) => new CustomLoadBalancer(discoveryProvider.GetAsync);
        var ports = PortFinder.GetPorts(2);
        var route = GivenRoute(nameof(CustomLoadBalancer), ports);
        var configuration = GivenConfiguration(route);
        var downstreamServiceUrls = ports.Select(DownstreamUrl).ToArray();
        GivenMultipleServiceInstancesAreRunning(downstreamServiceUrls);
        this.Given(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithCustomLoadBalancer(loadBalancerFactoryFunc))
            .When(x => WhenIGetUrlOnTheApiGatewayConcurrently("/", 50))
            .Then(x => x.ThenTheTwoServicesShouldHaveBeenCalledTimes(50))

            // Quite risky assertion because the actual values based on health checks and threading
            .And(x => x.ThenBothServicesCalledRealisticAmountOfTimes(1, 49)) // (24, 26)
            .BDDfy();
    }

    private class CustomLoadBalancer : ILoadBalancer
    {
        private readonly Func<Task<List<Service>>> _services;
        private readonly object _lock = new();

        private int _last;

        public CustomLoadBalancer(Func<Task<List<Service>>> services)
        {
            _services = services;
        }

        public async Task<Response<ServiceHostAndPort>> Lease(HttpContext httpContext)
        {
            var services = await _services();
            lock (_lock)
            {
                if (_last >= services.Count)
                {
                    _last = 0;
                }

                var next = services[_last];
                _last++;
                return new OkResponse<ServiceHostAndPort>(next.HostAndPort);
            }
        }

        public void Release(ServiceHostAndPort hostAndPort) { }
    }

    private void ThenBothServicesCalledRealisticAmountOfTimes(int bottom, int top)
    {
        _counters[0].ShouldBeInRange(bottom, top);
        _counters[1].ShouldBeInRange(bottom, top);
    }

    private void ThenTheTwoServicesShouldHaveBeenCalledTimes(int expected)
    {
        var total = _counters[0] + _counters[1];
        total.ShouldBe(expected);
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
