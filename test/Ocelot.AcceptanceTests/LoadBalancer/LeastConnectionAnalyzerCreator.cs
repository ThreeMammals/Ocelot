using Ocelot.Configuration;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery.Providers;

namespace Ocelot.AcceptanceTests.LoadBalancer;

internal sealed class LeastConnectionAnalyzerCreator : ILoadBalancerCreator
{
    // We need to adhere to the same implementations of RoundRobinCreator, which results in a significant design overhead, (until redesigned)
    public Response<ILoadBalancer> Create(DownstreamRoute route, IServiceDiscoveryProvider serviceProvider)
    {
        var loadBalancer = new LeastConnectionAnalyzer(
            serviceProvider.GetAsync,
            !string.IsNullOrEmpty(route.ServiceName) // if service discovery mode then use service name; otherwise use balancer key
                ? route.ServiceName
                : route.LoadBalancerKey);
        return new OkResponse<ILoadBalancer>(loadBalancer);
    }

    public string Type => nameof(LeastConnectionAnalyzer);
}
