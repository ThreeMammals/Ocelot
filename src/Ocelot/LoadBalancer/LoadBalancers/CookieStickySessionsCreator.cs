namespace Ocelot.LoadBalancer.LoadBalancers
{
    using System.Threading.Tasks;
    using Ocelot.Configuration;
    using Ocelot.Infrastructure;
    using Ocelot.ServiceDiscovery.Providers;

    public class CookieStickySessionsCreator : ILoadBalancerCreator
    {
        public ILoadBalancer Create(DownstreamReRoute reRoute, IServiceDiscoveryProvider serviceProvider)
        {
            var loadBalancer = new RoundRobin(async () => await serviceProvider.Get());
            var bus = new InMemoryBus<StickySession>();
            return new CookieStickySessions(loadBalancer, reRoute.LoadBalancerOptions.Key,
                reRoute.LoadBalancerOptions.ExpiryInMs, bus);
        }

        public string Type => nameof(CookieStickySessions);
    }
}
