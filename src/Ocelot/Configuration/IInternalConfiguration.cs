using System.Collections.Generic;

namespace Ocelot.Configuration
{
    public interface IInternalConfiguration
    {
        List<ReRoute> ReRoutes { get; }

        string AdministrationPath {get;}

        ServiceProviderConfiguration ServiceProviderConfiguration {get;}

        DynamicReRouteConfiguration DynamicReRouteConfiguration { get; }

        string RequestId {get;}

        LoadBalancerOptions LoadBalancerOptions { get; }

        string DownstreamScheme { get; }

        QoSOptions QoSOptions { get; }

        RateLimitGlobalOptions RateLimitOptions { get; }

        HttpHandlerOptions HttpHandlerOptions { get; }
    }
}
