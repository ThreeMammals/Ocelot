using System.Threading.Tasks;
using Ocelot.Configuration;
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

        public async Task<ILoadBalancer> Get(ReRoute reRoute)
        {            
            var serviceProvider = _serviceProviderFactory.Get(reRoute.ServiceProviderConfiguraion);

            switch (reRoute.LoadBalancer)
            {
                case "RoundRobin":
                    return new RoundRobinLoadBalancer(await serviceProvider.Get());
                case "LeastConnection":
                    return new LeastConnectionLoadBalancer(async () => await serviceProvider.Get(), reRoute.ServiceProviderConfiguraion.ServiceName);
                default:
                    return new NoLoadBalancer(await serviceProvider.Get());
            }
        }
    }
}
