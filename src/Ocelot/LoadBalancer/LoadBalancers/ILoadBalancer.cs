using Ocelot.Middleware;
using Ocelot.Responses;
using Ocelot.Values;
using System.Threading.Tasks;

namespace Ocelot.LoadBalancer.LoadBalancers
{
    public interface ILoadBalancer
    {
        Task<Response<ServiceHostAndPort>> Lease(DownstreamContext context);

        void Release(ServiceHostAndPort hostAndPort);
    }
}
