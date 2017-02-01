using Ocelot.ServiceDiscovery;

namespace Ocelot.LoadBalancer.LoadBalancers
{
    public class LoadBalancerFactory : ILoadBalancerFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public LoadBalancerFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ILoadBalancer Get(string serviceName, string loadBalancer)
        {
            switch (loadBalancer)
            {
                case "RoundRobin":
                    return new RoundRobinLoadBalancer(_serviceProvider.Get());
                case "LeastConnection":
                    return new LeastConnectionLoadBalancer(() => _serviceProvider.Get(), serviceName);
                default:
                    return new NoLoadBalancer(_serviceProvider.Get());
            }
        }
    }
}
