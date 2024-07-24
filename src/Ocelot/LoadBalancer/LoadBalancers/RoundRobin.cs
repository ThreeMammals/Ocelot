using Microsoft.AspNetCore.Http;
using Ocelot.Responses;
using Ocelot.Values;

namespace Ocelot.LoadBalancer.LoadBalancers
{
    public class RoundRobin : ILoadBalancer
    {
        private readonly Func<Task<List<Service>>> _servicesDelegate;
        private readonly string _serviceName;

        public RoundRobin(Func<Task<List<Service>>> services, string serviceName)
        {
            _servicesDelegate = services;
            _serviceName = serviceName;
        }

        private static readonly object _lock = new();
        public static object SyncRoot => _lock;

        //public static int Last => _last;
        //private /*static*/ int last;
        private static readonly Dictionary<string, int> LastIndices = new();

        public virtual async Task<Response<ServiceHostAndPort>> Lease(HttpContext httpContext)
        {
            var services = await _servicesDelegate?.Invoke() ?? new List<Service>();
            if (services.Count == 0)
            {
                return new ErrorResponse<ServiceHostAndPort>(new ServicesAreEmptyError($"There were no services in {nameof(RoundRobin)} for {_serviceName} during {nameof(Lease)} operation."));
            }

            lock (_lock)
            {
                int count = services.Count; // capture the count value because another thread might modify the list
                LastIndices.TryGetValue(_serviceName, out int last);
                if (last >= count)
                {
                    last = 0;
                }

                var index = last;
                var next = services[last];
                LastIndices[_serviceName] = (++last < count) ? last : 0;

                if (next == null)
                {
                    return new ErrorResponse<ServiceHostAndPort>(new ServicesAreNullError($"The service at index {index} was null in {nameof(RoundRobin)} for {_serviceName} during the {nameof(Lease)} operation. Total services count: {count}."));
                }
                else if (next.HostAndPort == null)
                {
                    return new ErrorResponse<ServiceHostAndPort>(new ServicesAreNullError($"The {nameof(next.HostAndPort)} was null for the service at index {index} in {nameof(RoundRobin)} for {_serviceName} during the {nameof(Lease)} operation. Total services count: {count}."));
                }

                // Happy path: Lease now
                return new OkResponse<ServiceHostAndPort>(next.HostAndPort);
            }
        }

        public virtual void Release(ServiceHostAndPort hostAndPort)
        {
            // TODO Add lease map
        }
    }
}
