using Ocelot.Configuration;
using Ocelot.LoadBalancer.Balancers;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery.Providers;

namespace Ocelot.LoadBalancer.Creators;

public class LeastConnectionCreator : ILoadBalancerCreator
{
    public Response<ILoadBalancer> Create(DownstreamRoute route, IServiceDiscoveryProvider serviceProvider)
    {
        var loadBalancer = new LeastConnection(
            serviceProvider.GetAsync,
            !string.IsNullOrEmpty(route.ServiceName)
                ? route.ServiceName
                : route.LoadBalancerKey); // if service discovery mode then use service name; otherwise use balancer key
        return new OkResponse<ILoadBalancer>(loadBalancer);
    }

    public string Type => nameof(LeastConnection);
}
