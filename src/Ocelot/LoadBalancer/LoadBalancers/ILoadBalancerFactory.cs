namespace Ocelot.LoadBalancer.LoadBalancers
{
    using Configuration;
    using Responses;

    public interface ILoadBalancerFactory
    {
        Response<ILoadBalancer> Get(DownstreamRoute route, ServiceProviderConfiguration config);
    }
}
