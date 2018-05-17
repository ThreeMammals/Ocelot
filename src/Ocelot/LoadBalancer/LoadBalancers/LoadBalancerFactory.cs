using System.Threading.Tasks;
using Ocelot.Configuration;
using Ocelot.Infrastructure;
using Ocelot.ServiceDiscovery;

namespace Ocelot.LoadBalancer.LoadBalancers
{
    public class LoadBalancerFactory : ILoadBalancerFactory
    {
        private readonly IServiceDiscoveryProviderFactory _serviceProviderFactory;

        public LoadBalancerFactory(IServiceDiscoveryProviderFactory serviceProviderFactory)
        {
            _serviceProviderFactory = serviceProviderFactory;
        }

        public async Task<ILoadBalancer> Get(DownstreamReRoute reRoute, ServiceProviderConfiguration config)
        {            
            var serviceProvider = _serviceProviderFactory.Get(config, reRoute);

            switch (reRoute.LoadBalancerOptions?.Type)
            {
                case nameof(RoundRobin):
                    return new RoundRobin(async () => await serviceProvider.Get());
                case nameof(LeastConnection):
                    return new LeastConnection(async () => await serviceProvider.Get(), reRoute.ServiceName);
                case nameof(CookieStickySessions):
                    var loadBalancer = new RoundRobin(async () => await serviceProvider.Get());
                    var bus = new InMemoryBus<StickySession>();
                    return new CookieStickySessions(loadBalancer, reRoute.LoadBalancerOptions.Key, reRoute.LoadBalancerOptions.ExpiryInMs, bus);
                default:
                    return new NoLoadBalancer(async () => await serviceProvider.Get());
            }
        }
    }
}
