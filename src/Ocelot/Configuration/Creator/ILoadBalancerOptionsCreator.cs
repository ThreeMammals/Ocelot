using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public interface ILoadBalancerOptionsCreator
    {
        LoadBalancerOptions CreateLoadBalancerOptions(FileLoadBalancerOptions options);
    }
}
