using Ocelot.Configuration;
using Ocelot.Responses;

namespace Ocelot.LoadBalancer.Balancers;

public interface ILoadBalancerFactory
{
    Response<ILoadBalancer> Get(DownstreamRoute route, ServiceProviderConfiguration config);
}
