using System.Collections.Generic;
using System.Threading.Tasks;
using Ocelot.Responses;
using Ocelot.Values;

namespace Ocelot.LoadBalancer.LoadBalancers
{
    public class RoundRobinLoadBalancer : ILoadBalancer
    {
        private readonly List<Service> _services;
        private int _last;

        public RoundRobinLoadBalancer(List<Service> services)
        {
            _services = services;
        }

        public async Task<Response<HostAndPort>> Lease()
        {
            if (_last >= _services.Count)
            {
                _last = 0;
            }

            var next = await Task.FromResult(_services[_last]);
            _last++;
            return new OkResponse<HostAndPort>(next.HostAndPort);
        }

        public void Release(HostAndPort hostAndPort)
        {
        }
    }
}
