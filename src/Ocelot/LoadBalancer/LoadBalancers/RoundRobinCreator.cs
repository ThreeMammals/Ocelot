using Ocelot.Configuration;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery.Providers;

namespace Ocelot.LoadBalancer.LoadBalancers;

public class RoundRobinCreator : ILoadBalancerCreator
{
    public Response<ILoadBalancer> Create(DownstreamRoute route, IServiceDiscoveryProvider serviceProvider)
    {
        var loadBalancer = new RoundRobin(
            serviceProvider.GetAsync,
            !string.IsNullOrEmpty(route.ServiceName)
                ? route.ServiceName
                : route.LoadBalancerKey); // if service discovery mode then use service name; otherwise use balancer key
        return new OkResponse<ILoadBalancer>(loadBalancer);
    }

    public string Type => nameof(RoundRobin);
}
