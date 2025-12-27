using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.LoadBalancer.Balancers;
using Ocelot.LoadBalancer.Creators;
using Ocelot.ServiceDiscovery.Providers;

namespace Ocelot.UnitTests.LoadBalancer;

public class CookieStickySessionsCreatorTests : UnitTest
{
    private readonly CookieStickySessionsCreator _creator;
    private readonly Mock<IServiceDiscoveryProvider> _serviceProvider;

    public CookieStickySessionsCreatorTests()
    {
        _creator = new();
        _serviceProvider = new();
    }

    [Fact]
    public void Should_return_instance_of_expected_load_balancer_type()
    {
        // Arrange
        var route = new DownstreamRouteBuilder()
            .WithLoadBalancerOptions(new LoadBalancerOptions("myType", "myKey", 1000))
            .Build();

        // Act
        var loadBalancer = _creator.Create(route, _serviceProvider.Object);

        // Assert
        loadBalancer.Data.ShouldBeOfType<CookieStickySessions>();
    }

    [Fact]
    public void Should_return_expected_name()
    {
        // Arrange, Act, Assert
        _creator.Type.ShouldBe(nameof(CookieStickySessions));
    }
}
