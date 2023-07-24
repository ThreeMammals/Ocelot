using Microsoft.AspNetCore.Http;
using Ocelot.Responses;
using Ocelot.Values;
using System.Threading.Tasks;

namespace Ocelot.LoadBalancer.LoadBalancers
{
    public interface ILoadBalancer
    {
        Task<Response<ServiceHostAndPort>> Lease(HttpContext httpContext);

        void Release(ServiceHostAndPort hostAndPort);
    }
}
