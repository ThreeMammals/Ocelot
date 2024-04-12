using Ocelot.Configuration;
using Ocelot.Errors;
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

        public Response<ILoadBalancer> Get(DownstreamRoute route, ServiceProviderConfiguration config)
        {
            try
            {
                if (_loadBalancers.TryGetValue(route.LoadBalancerKey, out var loadBalancer))
                {
                    // TODO Fix ugly reflection issue of dymanic detection in favor of static type property
                    if (route.LoadBalancerOptions.Type != loadBalancer.GetType().Name)
                    {
                        return GetResponse(route, config);
                    }

                    return new OkResponse<ILoadBalancer>(loadBalancer);
                }

                return GetResponse(route, config);
            }
            catch (Exception ex)
            {
                return new ErrorResponse<ILoadBalancer>(new List<Error>()
                {
                    new UnableToFindLoadBalancerError($"Unable to find load balancer for '{route.LoadBalancerKey}'. Exception: {ex};"),
                });
            }
        }

        private Response<ILoadBalancer> GetResponse(DownstreamRoute route, ServiceProviderConfiguration config)
        {
            var result = _factory.Get(route, config);

            if (result.IsError)
            {
                return new ErrorResponse<ILoadBalancer>(result.Errors);
            }

            var loadBalancer = result.Data;
            AddLoadBalancer(route.LoadBalancerKey, loadBalancer);
            return new OkResponse<ILoadBalancer>(loadBalancer);
        }

        private void AddLoadBalancer(string key, ILoadBalancer loadBalancer)
        {
            _loadBalancers.AddOrUpdate(key, loadBalancer, (x, y) => loadBalancer);
        }
    }
}
