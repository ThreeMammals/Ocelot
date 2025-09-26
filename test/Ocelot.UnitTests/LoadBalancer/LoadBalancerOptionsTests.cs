using Ocelot.Configuration;
using Ocelot.LoadBalancer.Balancers;

namespace Ocelot.UnitTests.LoadBalancer;

public class LoadBalancerOptionsTests
{
    [Fact]
    public void Ctor_ShouldDefaultToNoLoadBalancer()
    {
        // Arrange, Act
        LoadBalancerOptions options = new();
        LoadBalancerOptions options2 = new(default, default, default);

        // Assert
        options.Type.ShouldBe(nameof(NoLoadBalancer));
        options2.Type.ShouldBe(nameof(NoLoadBalancer));
    }
}
