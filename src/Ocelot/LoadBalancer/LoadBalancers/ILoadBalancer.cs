using Microsoft.AspNetCore.Http;
using Ocelot.Responses;
using Ocelot.Values;

namespace Ocelot.LoadBalancer.LoadBalancers
{
    public interface ILoadBalancer
    {
        Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext);

        void Release(ServiceHostAndPort hostAndPort);
    }
}
