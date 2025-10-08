using Ocelot.Configuration;
using Ocelot.LoadBalancer.Errors;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;

namespace Ocelot.LoadBalancer;

public class LoadBalancerHouse : ILoadBalancerHouse
{
    private readonly ILoadBalancerFactory _factory;
    private readonly Dictionary<string, ILoadBalancer> _loadBalancers;
#if NET9_0_OR_GREATER
    private static readonly Lock SyncRoot = new();
#else
    private static readonly object SyncRoot = new();
#endif

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
                return _loadBalancers.TryGetValue(route.LoadBalancerKey, out var loadBalancer) &&
                        loadBalancer.Type.Equals(route.LoadBalancerOptions.Type, StringComparison.InvariantCultureIgnoreCase)
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
