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

        public async Task<ILoadBalancer> Get(ReRoute reRoute, ServiceProviderConfiguration config)
        {            
            var serviceProvider = _serviceProviderFactory.Get(config, reRoute);

            switch (reRoute.LoadBalancer)
            {
                case "RoundRobin":
                    return new RoundRobin(async () => await serviceProvider.Get());
                case "LeastConnection":
                    return new LeastConnection(async () => await serviceProvider.Get(), reRoute.ServiceName);
                default:
                    return new NoLoadBalancer(await serviceProvider.Get());
            }
        }
    }
}
