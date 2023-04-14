namespace Ocelot.LoadBalancer.LoadBalancers
{
    using Configuration;
    using Responses;
    using ServiceDiscovery.Providers;

    public class RoundRobinCreator : ILoadBalancerCreator
    {
        public Response<ILoadBalancer> Create(DownstreamRoute route, IServiceDiscoveryProvider serviceProvider)
        {
            return new OkResponse<ILoadBalancer>(new RoundRobin(async () => await serviceProvider.Get()));
        }

        public string Type => nameof(RoundRobin);
    }
}
