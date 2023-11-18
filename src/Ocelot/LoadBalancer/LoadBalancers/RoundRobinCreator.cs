using Ocelot.Configuration;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery.Providers;

namespace Ocelot.LoadBalancer.LoadBalancers
{
    public class RoundRobinCreator : ILoadBalancerCreator
    {
        public Response<ILoadBalancer> Create(DownstreamRoute route, IServiceDiscoveryProvider serviceProvider)
        {
            return new OkResponse<ILoadBalancer>(new RoundRobin(async () => await serviceProvider.GetAsync()));
        }

        public string Type => nameof(RoundRobin);
    }
}
