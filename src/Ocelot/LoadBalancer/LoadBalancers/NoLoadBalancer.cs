using Microsoft.AspNetCore.Http;
using Ocelot.Responses;
using Ocelot.Values;

namespace Ocelot.LoadBalancer.LoadBalancers
{
    public class NoLoadBalancer : ILoadBalancer
    {
        private readonly Func<Task<List<Service>>> _services;

        public NoLoadBalancer(Func<Task<List<Service>>> services)
        {
            _services = services;
        }

        public async Task<Response<ServiceHostAndPort>> Lease(HttpContext httpContext)
        {
            var services = await _services();

            if (services == null || services.Count == 0)
            {
                return new ErrorResponse<ServiceHostAndPort>(new ServicesAreEmptyError("There were no services in NoLoadBalancer"));
            }

            var service = await Task.FromResult(services.FirstOrDefault());
            return new OkResponse<ServiceHostAndPort>(service.HostAndPort);
        }

        public void Release(ServiceHostAndPort hostAndPort)
        {
        }
    }
}
