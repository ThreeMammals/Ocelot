namespace Ocelot.LoadBalancer.LoadBalancers
{
    using Ocelot.Configuration;
    using Ocelot.ServiceDiscovery.Providers;
    
    public class RoundRobinCreator : ILoadBalancerCreator
    {
        public ILoadBalancer Create(DownstreamReRoute reRoute, IServiceDiscoveryProvider serviceProvider)
        {
            return new RoundRobin(async () => await serviceProvider.Get());
        }

        public string Type => nameof(RoundRobin);
    }
}
