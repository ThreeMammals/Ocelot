using System.Threading.Tasks;
using Ocelot.Middleware;
using Ocelot.Responses;
using Ocelot.Values;

namespace Ocelot.LoadBalancer.LoadBalancers
{
    public interface ILoadBalancer
    {
        Task<Response<ServiceHostAndPort>> Lease(DownstreamContext context);
        void Release(ServiceHostAndPort hostAndPort);
    }
}
