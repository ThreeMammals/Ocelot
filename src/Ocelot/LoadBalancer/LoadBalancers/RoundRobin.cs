using Microsoft.AspNetCore.Http;
using Ocelot.Responses;
using Ocelot.Values;

namespace Ocelot.LoadBalancer.LoadBalancers
{
    public class RoundRobin : ILoadBalancer
    {
        private readonly Func<Task<List<Service>>> _servicesDelegate;
        private readonly object _lock = new();

        private int _last;

        public RoundRobin(Func<Task<List<Service>>> services)
        {
            _servicesDelegate = services;
        }

        public async Task<Response<ServiceHostAndPort>> Lease(HttpContext httpContext)
        {
            var services = await _servicesDelegate?.Invoke() ?? new List<Service>();

            if (services?.Count != 0)
            {
                lock (_lock)
                {
                    if (_last >= services.Count)
                    {
                        _last = 0;
                    }

                    var next = services[_last++];

                    if (next == null)
                    {
                        return new ErrorResponse<ServiceHostAndPort>(new ServicesAreNullError($"The service with index {_last} was null in {nameof(RoundRobin)} during the {nameof(Lease)} operation. Total services count: {services.Count}."));
                    }

                    if (next.HostAndPort == null)
                    {
                        return new ErrorResponse<ServiceHostAndPort>(new ServicesAreNullError($"The {nameof(next.HostAndPort)} was null in the service with index {_last} in {nameof(RoundRobin)} during the {nameof(Lease)} operation. Total services count: {services.Count}."));
                    }

                    return new OkResponse<ServiceHostAndPort>(next.HostAndPort);
                }
            }

            return new ErrorResponse<ServiceHostAndPort>(new ServicesAreEmptyError($"There were no services in {nameof(RoundRobin)} during {nameof(Lease)} operation."));
        }

        public void Release(ServiceHostAndPort hostAndPort)
        {
        }
    }
}
