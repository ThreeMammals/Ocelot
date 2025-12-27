using Ocelot.Configuration.Builder;
using Ocelot.LoadBalancer.Balancers;
using Ocelot.LoadBalancer.Creators;
using Ocelot.ServiceDiscovery.Providers;
using System.Reflection;

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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Should_return_instance_of_expected_load_balancer_type(bool isNullServiceName)
    {
        // Arrange
        var route = new DownstreamRouteBuilder()
            .WithServiceName(isNullServiceName ? null : "myService")
            .WithLoadBalancerKey("key")
            .Build();

        // Act
        var response = _creator.Create(route, _serviceProvider.Object);

        // Assert
        response.Data.ShouldBeOfType<LeastConnection>();
        var balancer = response.Data as LeastConnection;
        var field = balancer.GetType().GetField("_serviceName", BindingFlags.Instance | BindingFlags.NonPublic);
        var serviceName = field.GetValue(balancer) as string;
        serviceName.ShouldBe(isNullServiceName ? "key" : "myService");
    }

    [Fact]
    public void Should_return_expected_name()
    {
        // Arrange, Act, Assert
        _creator.Type.ShouldBe(nameof(LeastConnection));
    }
}
