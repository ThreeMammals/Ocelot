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

        public async Task<Response<ILoadBalancer>> Get(ReRoute reRoute, ServiceProviderConfiguration config)
        {
            try
            {
                ILoadBalancer loadBalancer;

                if(_loadBalancers.TryGetValue(reRoute.ReRouteKey, out loadBalancer))
                {
                    loadBalancer = _loadBalancers[reRoute.ReRouteKey];

                    //todo - we have some duplicate namey type logic in the LoadBalancerFactory...maybe we can do something
                    //about this..
                    if((reRoute.LoadBalancer == "RoundRobin" && loadBalancer.GetType() != typeof(RoundRobinLoadBalancer))
                        || (reRoute.LoadBalancer == "LeastConnection" && loadBalancer.GetType() != typeof(LeastConnectionLoadBalancer)))
                    {
                        loadBalancer = await _factory.Get(reRoute, config);
                        AddLoadBalancer(reRoute.ReRouteKey, loadBalancer);
                    }

                    return new OkResponse<ILoadBalancer>(loadBalancer);
                }

                loadBalancer = await _factory.Get(reRoute, config);
                AddLoadBalancer(reRoute.ReRouteKey, loadBalancer);
                return new OkResponse<ILoadBalancer>(loadBalancer);
            }
            catch(Exception ex)
            {
                return new ErrorResponse<ILoadBalancer>(new List<Ocelot.Errors.Error>()
                {
                    new UnableToFindLoadBalancerError($"unabe to find load balancer for {reRoute.ReRouteKey} exception is {ex}")
                });
            }
        }

        private void AddLoadBalancer(string key, ILoadBalancer loadBalancer)
        {
            _loadBalancers.AddOrUpdate(key, loadBalancer, (x, y) => {
                return loadBalancer;
            });
            
            // if (!_loadBalancers.ContainsKey(key))
            // {
            //     _loadBalancers.TryAdd(key, loadBalancer);
            // }

            // ILoadBalancer old;
            // _loadBalancers.Remove(key, out old);
            // _loadBalancers.TryAdd(key, loadBalancer);
        }
    }
}
