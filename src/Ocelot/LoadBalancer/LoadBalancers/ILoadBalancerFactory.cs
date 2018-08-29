namespace Ocelot.LoadBalancer.LoadBalancers
{
    using System.Threading.Tasks;
    using Ocelot.Configuration;
    using Ocelot.Responses;

    public interface ILoadBalancerFactory
    {
        Task<Response<ILoadBalancer>> Get(DownstreamReRoute reRoute, ServiceProviderConfiguration config);
    }
}
