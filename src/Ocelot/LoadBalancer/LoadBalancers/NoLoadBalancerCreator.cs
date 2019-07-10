namespace Ocelot.LoadBalancer.LoadBalancers
{ 
    using Ocelot.Configuration;
    using Ocelot.ServiceDiscovery.Providers;

    public class NoLoadBalancerCreator : ILoadBalancerCreator
    {
        public ILoadBalancer Create(DownstreamReRoute reRoute, IServiceDiscoveryProvider serviceProvider)
        {
            return new NoLoadBalancer(async () => await serviceProvider.Get());
        }

        public string Type => nameof(NoLoadBalancer);
    }
}
