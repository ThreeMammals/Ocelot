using Ocelot.Configuration;
using Ocelot.LoadBalancer.LoadBalancers;

namespace Ocelot.UnitTests.LoadBalancer;

public class LoadBalancerOptionsTests
{
    [Fact]
    public void Should_default_to_no_load_balancer()
    {
        var options = new LoadBalancerOptionsBuilder().Build();
        options.Type.ShouldBe(nameof(NoLoadBalancer));
    }
}
