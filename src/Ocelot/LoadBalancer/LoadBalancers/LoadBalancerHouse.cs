using System.Collections.Generic;
using Ocelot.Responses;

namespace Ocelot.LoadBalancer.LoadBalancers
{
    public class LoadBalancerHouse : ILoadBalancerHouse
    {
        private readonly Dictionary<string, ILoadBalancer> _loadBalancers;

        public LoadBalancerHouse()
        {
            _loadBalancers = new Dictionary<string, ILoadBalancer>();
        }

        public Response<ILoadBalancer> Get(string key)
        {
            ILoadBalancer loadBalancer;

            if(_loadBalancers.TryGetValue(key, out loadBalancer))
            {
                return new OkResponse<ILoadBalancer>(_loadBalancers[key]);
            }

                return new ErrorResponse<ILoadBalancer>(new List<Ocelot.Errors.Error>()
            {
                new UnableToFindLoadBalancerError($"unabe to find load balancer for {key}")
            });
        }

        public Response Add(string key, ILoadBalancer loadBalancer)
        {
            if (!_loadBalancers.ContainsKey(key))
            {
                _loadBalancers.Add(key, loadBalancer);
            }

            _loadBalancers.Remove(key);
            _loadBalancers.Add(key, loadBalancer);
            return new OkResponse();
        }
    }
}
