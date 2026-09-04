using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public interface ILoadBalancerOptionsCreator
{
    LoadBalancerOptions Create(FileLoadBalancerOptions options);
    LoadBalancerOptions Create(FileRoute route, FileGlobalConfiguration globalConfiguration);
    LoadBalancerOptions Create(FileDynamicRoute route, FileGlobalConfiguration globalConfiguration);
}
