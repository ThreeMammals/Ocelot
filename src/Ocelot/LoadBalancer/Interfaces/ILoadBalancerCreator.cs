using Ocelot.Configuration;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery.Providers;

namespace Ocelot.LoadBalancer.Interfaces;

public interface ILoadBalancerCreator
{
    Response<ILoadBalancer> Create(DownstreamRoute route, IServiceDiscoveryProvider serviceProvider);
    string Type { get; }
}
