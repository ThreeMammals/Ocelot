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
            var options = route.LoadBalancerOptions;
            var loadBalancer = new RoundRobin(serviceProvider.GetAsync, route.LoadBalancerKey);
            var bus = new InMemoryBus<StickySession>();
            return new OkResponse<ILoadBalancer>(
                new CookieStickySessions(loadBalancer, options.Key, options.ExpiryInMs, bus));
        }

        public string Type => nameof(CookieStickySessions);
    }
}
