using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public interface IRouteKeyCreator
{
    string Create(FileRoute route, LoadBalancerOptions loadBalancing);
    string Create(FileDynamicRoute route, LoadBalancerOptions loadBalancing);
    string Create(string serviceNamespace, string serviceName, LoadBalancerOptions loadBalancing);
}
