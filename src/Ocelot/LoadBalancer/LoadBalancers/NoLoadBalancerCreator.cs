namespace Ocelot.LoadBalancer.LoadBalancers
{
    using Configuration;
    using Responses;
    using ServiceDiscovery.Providers;

    public class NoLoadBalancerCreator : ILoadBalancerCreator
    {
        public Response<ILoadBalancer> Create(DownstreamRoute route, IServiceDiscoveryProvider serviceProvider)
        {
            return new OkResponse<ILoadBalancer>(new NoLoadBalancer(async () => await serviceProvider.Get()));
        }

        public string Type => nameof(NoLoadBalancer);
    }
}
