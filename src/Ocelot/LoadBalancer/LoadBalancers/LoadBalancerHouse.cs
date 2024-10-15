using Ocelot.Configuration;
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
                    return (_loadBalancers.TryGetValue(route.LoadBalancerKey, out var loadBalancer) &&
                            route.LoadBalancerOptions.Type == loadBalancer.Type) // TODO Case insensitive?
                        ? new OkResponse<ILoadBalancer>(loadBalancer)
                        : GetResponse(route, config);
                }
            }
            catch (Exception ex)
            {
                return new ErrorResponse<ILoadBalancer>(
                    new UnableToFindLoadBalancerError($"Unable to find load balancer for '{route.LoadBalancerKey}'. Exception: {ex};"));
            }
        }

        private Response<ILoadBalancer> GetResponse(DownstreamRoute route, ServiceProviderConfiguration config)
        {
            var result = _factory.Get(route, config);
            if (result.IsError)
            {
                return new ErrorResponse<ILoadBalancer>(result.Errors);
            }

            var balancer = result.Data;
            _loadBalancers[route.LoadBalancerKey] = balancer; // TODO TryAdd ?
            return new OkResponse<ILoadBalancer>(balancer);
        }
    }
}
