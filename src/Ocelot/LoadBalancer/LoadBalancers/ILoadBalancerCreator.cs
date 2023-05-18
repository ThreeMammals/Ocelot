namespace Ocelot.LoadBalancer.LoadBalancers
{
    using Configuration;

    using Responses;

    using ServiceDiscovery.Providers;

    public interface ILoadBalancerCreator
    {
        Response<ILoadBalancer> Create(DownstreamRoute route, IServiceDiscoveryProvider serviceProvider);
        string Type { get; }
    }
}
