namespace Ocelot.LoadBalancer.LoadBalancers
{
    using Microsoft.AspNetCore.Http;
    using Ocelot.Responses;
    using Ocelot.Values;
    using System.Threading.Tasks;
    using Ocelot.Middleware;

    public interface ILoadBalancer
    {
        Task<Response<ServiceHostAndPort>> Lease(DownstreamContext downstreamContext, HttpContext httpContext);

        void Release(ServiceHostAndPort hostAndPort);
    }
}
