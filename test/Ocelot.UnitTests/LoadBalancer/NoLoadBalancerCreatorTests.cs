using Ocelot.Configuration.Builder;
using Ocelot.LoadBalancer.Balancers;
using Ocelot.ServiceDiscovery.Providers;

namespace Ocelot.UnitTests.LoadBalancer;

public class NoLoadBalancerCreatorTests : UnitTest
{
    private readonly NoLoadBalancerCreator _creator;
    private readonly Mock<IServiceDiscoveryProvider> _serviceProvider;

    public NoLoadBalancerCreatorTests()
    {
        _creator = new NoLoadBalancerCreator();
        _serviceProvider = new Mock<IServiceDiscoveryProvider>();
    }

    [Fact]
    public void Should_return_instance_of_expected_load_balancer_type()
    {
        // Arrange
        var route = new DownstreamRouteBuilder().Build();

        // Act
        var loadBalancer = _creator.Create(route, _serviceProvider.Object);

        // Assert
        loadBalancer.Data.ShouldBeOfType<NoLoadBalancer>();
    }

    [Fact]
    public void Should_return_expected_name()
    {
        // Arrange, Act, Assert
        _creator.Type.ShouldBe(nameof(NoLoadBalancer));
    }
}
