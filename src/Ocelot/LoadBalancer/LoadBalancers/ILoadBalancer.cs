namespace Ocelot.LoadBalancer.LoadBalancers
{
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Http;

    using Responses;
    using Values;

    public interface ILoadBalancer
    {
        Task<Response<ServiceHostAndPort>> Lease(HttpContext httpContext);

        void Release(ServiceHostAndPort hostAndPort);
    }
}
