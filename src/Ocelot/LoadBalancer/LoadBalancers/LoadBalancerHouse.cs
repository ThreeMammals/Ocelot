using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ocelot.Configuration;
using Ocelot.Responses;

namespace Ocelot.LoadBalancer.LoadBalancers
{
    public class LoadBalancerHouse : ILoadBalancerHouse
    {
        private readonly ILoadBalancerFactory _factory;
        private readonly ConcurrentDictionary<string, ILoadBalancer> _loadBalancers;

        public LoadBalancerHouse(ILoadBalancerFactory factory)
        {
            _factory = factory;
            _loadBalancers = new ConcurrentDictionary<string, ILoadBalancer>();
        }

        public async Task<Response<ILoadBalancer>> Get(DownstreamReRoute reRoute, ServiceProviderConfiguration config)
        {
            try
            {
                if(_loadBalancers.TryGetValue(reRoute.LoadBalancerKey, out var loadBalancer))
                {
                    loadBalancer = _loadBalancers[reRoute.LoadBalancerKey];

                    if(reRoute.LoadBalancerOptions.Type != loadBalancer.GetType().Name)
                    {
                        loadBalancer = await _factory.Get(reRoute, config);
                        AddLoadBalancer(reRoute.LoadBalancerKey, loadBalancer);
                    }

                    return new OkResponse<ILoadBalancer>(loadBalancer);
                }

                loadBalancer = await _factory.Get(reRoute, config);
                AddLoadBalancer(reRoute.LoadBalancerKey, loadBalancer);
                return new OkResponse<ILoadBalancer>(loadBalancer);
            }
            catch(Exception ex)
            {
                return new ErrorResponse<ILoadBalancer>(new List<Ocelot.Errors.Error>()
                {
                    new UnableToFindLoadBalancerError($"unabe to find load balancer for {reRoute.LoadBalancerKey} exception is {ex}")
                });
            }
        }

        private void AddLoadBalancer(string key, ILoadBalancer loadBalancer)
        {
            _loadBalancers.AddOrUpdate(key, loadBalancer, (x, y) => loadBalancer);
        }
    }
}
