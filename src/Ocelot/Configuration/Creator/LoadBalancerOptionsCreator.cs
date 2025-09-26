using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public class LoadBalancerOptionsCreator : ILoadBalancerOptionsCreator
{
    public LoadBalancerOptions Create(FileLoadBalancerOptions options)
    {
        return new(options.Type, options.Key, options.Expiry);
    }
}
