using System.Threading.Tasks;
using Ocelot.Configuration;
using Ocelot.Responses;

namespace Ocelot.LoadBalancer.LoadBalancers
{
    public interface ILoadBalancerHouse
    {
        Task<Response<ILoadBalancer>> Get(ReRoute reRoute, ServiceProviderConfiguration config);
    }
}