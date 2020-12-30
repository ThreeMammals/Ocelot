using Ocelot.Configuration;
using Ocelot.Infrastructure;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery.Providers;
using System.Threading.Tasks;

namespace Ocelot.LoadBalancer.LoadBalancers
{
    public class CookieStickySessionsCreator : ILoadBalancerCreator
    {
        private readonly IStickySessionStorage _sessionStorage;

        public CookieStickySessionsCreator(IStickySessionStorage sessionStorage)
        {
            _sessionStorage = sessionStorage;
        }

        public Response<ILoadBalancer> Create(DownstreamRoute route, IServiceDiscoveryProvider serviceProvider)
        {
            var options = route.LoadBalancerOptions;
            var loadBalancer = new RoundRobin(serviceProvider.GetAsync, route.LoadBalancerKey);
            var bus = new InMemoryBus<StickySession>();
            return new OkResponse<ILoadBalancer>(
                new CookieStickySessions(loadBalancer, options.Key, options.ExpiryInMs, bus, _sessionStorage));
        }

        public string Type => nameof(CookieStickySessions);
    }
}
