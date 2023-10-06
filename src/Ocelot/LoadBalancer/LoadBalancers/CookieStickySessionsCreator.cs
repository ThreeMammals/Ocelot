using Ocelot.Configuration;
using Ocelot.Infrastructure;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery.Providers;

namespace Ocelot.LoadBalancer.LoadBalancers
{
    public class CookieStickySessionsCreator : ILoadBalancerCreator
    {
        public Response<ILoadBalancer> Create(DownstreamRoute route, IServiceDiscoveryProvider serviceProvider)
        {
            var loadBalancer = new RoundRobin(async () => await serviceProvider.GetAsync());
            var bus = new InMemoryBus<StickySession>();
            return new OkResponse<ILoadBalancer>(new CookieStickySessions(loadBalancer, route.LoadBalancerOptions.Key,
                route.LoadBalancerOptions.ExpiryInMs, bus));
        }

        public string Type => nameof(CookieStickySessions);
    }
}
