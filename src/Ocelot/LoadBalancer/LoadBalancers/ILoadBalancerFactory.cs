namespace Ocelot.LoadBalancer.LoadBalancers
{
    using Ocelot.Configuration;
    using Ocelot.Responses;
    using System.Threading.Tasks;

    public interface ILoadBalancerFactory
    {
        Task<Response<ILoadBalancer>> Get(DownstreamReRoute reRoute, ServiceProviderConfiguration config);
    }
}
