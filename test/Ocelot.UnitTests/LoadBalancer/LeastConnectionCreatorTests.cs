using Ocelot.Configuration.Builder;
using Ocelot.LoadBalancer.Balancers;
using Ocelot.LoadBalancer.Creators;
using Ocelot.ServiceDiscovery.Providers;

namespace Ocelot.UnitTests.LoadBalancer;

public class LeastConnectionCreatorTests : UnitTest
{
    private readonly LeastConnectionCreator _creator;
    private readonly Mock<IServiceDiscoveryProvider> _serviceProvider;

    public LeastConnectionCreatorTests()
    {
        _creator = new();
        _serviceProvider = new();
    }

    [Fact]
    public void Should_return_instance_of_expected_load_balancer_type()
    {
        // Arrange
        var route = new DownstreamRouteBuilder()
            .WithServiceName("myService")
            .Build();

        // Act
        var loadBalancer = _creator.Create(route, _serviceProvider.Object);

        // Assert
        loadBalancer.Data.ShouldBeOfType<LeastConnection>();
    }

    [Fact]
    public void Should_return_expected_name()
    {
        // Arrange, Act, Assert
        _creator.Type.ShouldBe(nameof(LeastConnection));
    }
}
