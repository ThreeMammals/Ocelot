using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery.Providers;

namespace Ocelot.UnitTests.LoadBalancer;

public class RoundRobinCreatorTests : UnitTest
{
    private readonly RoundRobinCreator _creator;
    private readonly Mock<IServiceDiscoveryProvider> _serviceProvider;
    private DownstreamRoute _route;
    private Response<ILoadBalancer> _loadBalancer;
    private string _typeName;

    public RoundRobinCreatorTests()
    {
        _creator = new RoundRobinCreator();
        _serviceProvider = new Mock<IServiceDiscoveryProvider>();
    }

    [Fact]
    public void Should_return_instance_of_expected_load_balancer_type()
    {
        var route = new DownstreamRouteBuilder()
            .Build();

        GivenARoute(route);
        WhenIGetTheLoadBalancer();
        ThenTheLoadBalancerIsReturned<RoundRobin>();
    }

    [Fact]
    public void Should_return_expected_name()
    {
        WhenIGetTheLoadBalancerTypeName();
        ThenTheLoadBalancerTypeIs("RoundRobin");
    }

    private void GivenARoute(DownstreamRoute route)
    {
        _route = route;
    }

    private void WhenIGetTheLoadBalancer()
    {
        _loadBalancer = _creator.Create(_route, _serviceProvider.Object);
    }

    private void WhenIGetTheLoadBalancerTypeName()
    {
        _typeName = _creator.Type;
    }

    private void ThenTheLoadBalancerIsReturned<T>()
        where T : ILoadBalancer
    {
        _loadBalancer.Data.ShouldBeOfType<T>();
    }

    private void ThenTheLoadBalancerTypeIs(string type)
    {
        _typeName.ShouldBe(type);
    }
}
