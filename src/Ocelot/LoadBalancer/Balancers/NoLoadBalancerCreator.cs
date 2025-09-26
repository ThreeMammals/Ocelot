using Ocelot.Configuration;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery.Providers;

namespace Ocelot.LoadBalancer.Balancers;

public class NoLoadBalancerCreator : ILoadBalancerCreator
{
    public Response<ILoadBalancer> Create(DownstreamRoute route, IServiceDiscoveryProvider serviceProvider)
    {
        return new OkResponse<ILoadBalancer>(new NoLoadBalancer(async () => await serviceProvider.GetAsync()));
    }

    public string Type => nameof(NoLoadBalancer);
}
