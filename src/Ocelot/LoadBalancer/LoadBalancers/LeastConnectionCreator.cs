namespace Ocelot.LoadBalancer.LoadBalancers
{
    using Ocelot.Configuration;
    using Ocelot.ServiceDiscovery.Providers;

    public class LeastConnectionCreator : ILoadBalancerCreator
    {
        public ILoadBalancer Create(DownstreamReRoute reRoute, IServiceDiscoveryProvider serviceProvider)
        {
            return new LeastConnection(async () => await serviceProvider.Get(), reRoute.ServiceName);
        }

        public string Type => nameof(LeastConnection);
    }
}
