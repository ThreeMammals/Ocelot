namespace Ocelot.LoadBalancer.LoadBalancers
{
    using Ocelot.Configuration;
    using Ocelot.ServiceDiscovery.Providers;
    using Ocelot.Responses;

    public class LeastConnectionCreator : ILoadBalancerCreator
    {
        public Response<ILoadBalancer> Create(DownstreamReRoute reRoute, IServiceDiscoveryProvider serviceProvider)
        {
            return new OkResponse<ILoadBalancer>(new LeastConnection(async () => await serviceProvider.Get(), reRoute.ServiceName));
        }

        public string Type => nameof(LeastConnection);
    }
}
