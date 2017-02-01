using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ocelot.Responses;

namespace Ocelot.LoadBalancer.LoadBalancers
{
    public class LoadBalancerHouse
    {
        private readonly Dictionary<string, ILoadBalancer> _loadBalancers;

        public LoadBalancerHouse()
        {
            _loadBalancers = new Dictionary<string, ILoadBalancer>();
        }

        public Response<ILoadBalancer> Get(string key)
        {
            return new OkResponse<ILoadBalancer>(_loadBalancers[key]);
        }

        public Response Add(string key, ILoadBalancer loadBalancer)
        {
            _loadBalancers[key] = loadBalancer;
            return new OkResponse();
        }
    }
}
