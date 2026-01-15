using Ocelot.Configuration.Builder;
using Ocelot.LoadBalancer.Balancers;
using Ocelot.LoadBalancer.Creators;
using Ocelot.ServiceDiscovery.Providers;
using System.Reflection;

namespace Ocelot.UnitTests.LoadBalancer;

public class RoundRobinCreatorTests : UnitTest
{
    private readonly RoundRobinCreator _creator;
    private readonly Mock<IServiceDiscoveryProvider> _serviceProvider;

    public RoundRobinCreatorTests()
    {
        _creator = new RoundRobinCreator();
        _serviceProvider = new Mock<IServiceDiscoveryProvider>();
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
        var loadBalancer = _creator.Create(route, _serviceProvider.Object);

        // Assert
        loadBalancer.Data.ShouldBeOfType<RoundRobin>();
        var balancer = loadBalancer.Data as RoundRobin;
        var field = balancer.GetType().GetField("_serviceName", BindingFlags.Instance | BindingFlags.NonPublic);
        var serviceName = field.GetValue(balancer) as string;
        serviceName.ShouldBe(isNullServiceName ? "key" : "myService");
    }

    [Fact]
    public void Should_return_expected_name()
    {
        // Arrange, Act, Assert
        _creator.Type.ShouldBe(nameof(RoundRobin));
    }
}
