using System.Collections.Generic;
using System.Threading.Tasks;
using Ocelot.Responses;
using Ocelot.Values;
using System;
using Ocelot.Middleware;

namespace Ocelot.LoadBalancer.LoadBalancers
{
    public class RoundRobin : ILoadBalancer
    {
        private readonly Func<Task<List<Service>>> _services;

        private int _last;

        public RoundRobin(Func<Task<List<Service>>> services)
        {
            _services = services;
        }

        public async Task<Response<ServiceHostAndPort>> Lease(DownstreamContext downstreamContext)
        {
            var services = await _services();
            if (_last >= services.Count)
            {
                _last = 0;
            }

            var next = services[_last];
            _last++;
            return new OkResponse<ServiceHostAndPort>(next.HostAndPort);
        }

        public void Release(ServiceHostAndPort hostAndPort)
        {
        }
    }
}
