using Ocelot.Configuration;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery.Providers;

namespace Ocelot.LoadBalancer.LoadBalancers
{
    public class NoLoadBalancerCreator : ILoadBalancerCreator
    {
        public Response<ILoadBalancer> Create(DownstreamRoute route, IServiceDiscoveryProvider serviceProvider)
        {
            return new OkResponse<ILoadBalancer>(new NoLoadBalancer(async () => await serviceProvider.GetAsync()));
        }

        public string Type => nameof(NoLoadBalancer);
    }
}
