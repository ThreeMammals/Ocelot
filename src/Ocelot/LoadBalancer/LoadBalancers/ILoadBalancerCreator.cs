using Ocelot.Configuration;
using Ocelot.ServiceDiscovery.Providers;

namespace Ocelot.LoadBalancer.LoadBalancers
{
    public interface ILoadBalancerCreator
    {
        ILoadBalancer Create(DownstreamReRoute reRoute, IServiceDiscoveryProvider serviceProvider);
        string Type { get; }
    }
}
