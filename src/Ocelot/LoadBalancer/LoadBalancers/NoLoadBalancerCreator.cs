namespace Ocelot.LoadBalancer.LoadBalancers
{ 
    using Ocelot.Configuration;
    using Ocelot.ServiceDiscovery.Providers;
    using Ocelot.Responses;

    public class NoLoadBalancerCreator : ILoadBalancerCreator
    {
        public Response<ILoadBalancer> Create(DownstreamReRoute reRoute, IServiceDiscoveryProvider serviceProvider)
        {
            return new OkResponse<ILoadBalancer>(new NoLoadBalancer(async () => await serviceProvider.Get()));
        }

        public string Type => nameof(NoLoadBalancer);
    }
}
