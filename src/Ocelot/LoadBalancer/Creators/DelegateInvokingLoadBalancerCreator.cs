using Ocelot.Configuration;
using Ocelot.LoadBalancer.Errors;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery.Providers;

namespace Ocelot.LoadBalancer.Creators;

public class DelegateInvokingLoadBalancerCreator<T> : ILoadBalancerCreator
    where T : ILoadBalancer
{
    private readonly Func<DownstreamRoute, IServiceDiscoveryProvider, ILoadBalancer> _creatorFunc;

    public DelegateInvokingLoadBalancerCreator(
        Func<DownstreamRoute, IServiceDiscoveryProvider, ILoadBalancer> creatorFunc)
    {
        _creatorFunc = creatorFunc;
    }

    public Response<ILoadBalancer> Create(DownstreamRoute route, IServiceDiscoveryProvider serviceProvider)
    {
        try
        {
            return new OkResponse<ILoadBalancer>(_creatorFunc(route, serviceProvider));
        }
        catch (Exception e)
        {
            return new ErrorResponse<ILoadBalancer>(new InvokingLoadBalancerCreatorError(e));
        }
    }

    public string Type => typeof(T).Name;
}
