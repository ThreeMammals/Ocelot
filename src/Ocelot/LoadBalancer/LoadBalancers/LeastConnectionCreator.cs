namespace Ocelot.LoadBalancer.LoadBalancers
{
    using Configuration;
    using Responses;
    using ServiceDiscovery.Providers;

    public class LeastConnectionCreator : ILoadBalancerCreator
    {
        public Response<ILoadBalancer> Create(DownstreamRoute route, IServiceDiscoveryProvider serviceProvider)
        {
            return new OkResponse<ILoadBalancer>(new LeastConnection(async () => await serviceProvider.Get(), route.ServiceName));
        }

        public string Type => nameof(LeastConnection);
    }
}
