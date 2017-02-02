using Ocelot.Configuration;
using Ocelot.ServiceDiscovery;

namespace Ocelot.LoadBalancer.LoadBalancers
{
    public class LoadBalancerFactory : ILoadBalancerFactory
    {
        private readonly IServiceProviderFactory _serviceProviderFactory;
        public LoadBalancerFactory(IServiceProviderFactory serviceProviderFactory)
        {
            _serviceProviderFactory = serviceProviderFactory;
        }

        public ILoadBalancer Get(ReRoute reRoute)
        {
            var serviceConfig = new ServiceConfiguraion(
                reRoute.ServiceName,
                reRoute.DownstreamHost,
                reRoute.DownstreamPort,
                reRoute.UseServiceDiscovery);
            
            var serviceProvider = _serviceProviderFactory.Get(serviceConfig);

            switch (reRoute.LoadBalancer)
            {
                case "RoundRobin":
                    return new RoundRobinLoadBalancer(serviceProvider.Get());
                case "LeastConnection":
                    return new LeastConnectionLoadBalancer(() => serviceProvider.Get(), reRoute.ServiceName);
                default:
                    return new NoLoadBalancer(serviceProvider.Get());
            }
        }
    }
}
