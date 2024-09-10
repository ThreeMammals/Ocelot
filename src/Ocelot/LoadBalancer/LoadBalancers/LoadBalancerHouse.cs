using Ocelot.Configuration;
using Ocelot.Errors;
using Ocelot.Responses;

namespace Ocelot.LoadBalancer.LoadBalancers
{
    public class LoadBalancerHouse : ILoadBalancerHouse
    {
        private readonly ILoadBalancerFactory _factory;
        private readonly Dictionary<string, ILoadBalancer> _loadBalancers;
        private static readonly object SyncRoot = new();

        public LoadBalancerHouse(ILoadBalancerFactory factory)
        {
            _factory = factory;
            _loadBalancers = new();
        }

        public Response<ILoadBalancer> Get(DownstreamRoute route, ServiceProviderConfiguration config)
        {
            try
            {
                lock (SyncRoot)
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
            _loadBalancers[route.LoadBalancerKey] = loadBalancer; // TODO TryAdd ?
            return new OkResponse<ILoadBalancer>(loadBalancer);
        }
    }
}
