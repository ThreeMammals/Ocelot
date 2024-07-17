using Microsoft.AspNetCore.Http;
using Ocelot.Responses;
using Ocelot.Values;

namespace Ocelot.LoadBalancer.LoadBalancers
{
    public class RoundRobin : ILoadBalancer
    {
        private readonly Func<Task<List<Service>>> _servicesDelegate;

        public RoundRobin(Func<Task<List<Service>>> services)
        {
            _servicesDelegate = services;
        }

        private static readonly object _lock = new();
        public static object SyncRoot => _lock;

        private int _last;
        public int Last => _last;

        public virtual async Task<Response<ServiceHostAndPort>> Lease(HttpContext httpContext)
        {
            var services = await _servicesDelegate?.Invoke() ?? new List<Service>();
            if (services.Count == 0)
            {
                return new ErrorResponse<ServiceHostAndPort>(new ServicesAreEmptyError($"There were no services in {nameof(RoundRobin)} during {nameof(Lease)} operation."));
            }

            lock (_lock)
            {
                int count = services.Count; // capture the count value because another thread might modify the list
                if (_last >= count)
                {
                    _last = 0;
                }

                var next = services[_last];
                if (next == null)
                {
                    return new ErrorResponse<ServiceHostAndPort>(new ServicesAreNullError($"The service at index {_last} was null in {nameof(RoundRobin)} during the {nameof(Lease)} operation. Total services count: {count}."));
                }
                else if (next.HostAndPort == null)
                {
                    return new ErrorResponse<ServiceHostAndPort>(new ServicesAreNullError($"The {nameof(next.HostAndPort)} was null for the service at index {_last} in {nameof(RoundRobin)} during the {nameof(Lease)} operation. Total services count: {count}."));
                }

                // Happy path: Lease now
                _last++; // the Lease has been successful; advance the index.
                return new OkResponse<ServiceHostAndPort>(next.HostAndPort);
            }
        }

        public virtual void Release(ServiceHostAndPort hostAndPort)
        {
            // TODO Add lease map
        }
    }
}
