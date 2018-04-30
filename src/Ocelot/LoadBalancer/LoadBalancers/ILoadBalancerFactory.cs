using System.Threading.Tasks;
using Ocelot.Configuration;

namespace Ocelot.LoadBalancer.LoadBalancers
{
    public interface ILoadBalancerFactory
    {
        Task<ILoadBalancer> Get(DownstreamReRoute reRoute, ServiceProviderConfiguration config);
    }
}
