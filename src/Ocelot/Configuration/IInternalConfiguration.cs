using System.Collections.Generic;

namespace Ocelot.Configuration
{
    public interface IInternalConfiguration
    {
        List<ReRoute> ReRoutes { get; }

        string AdministrationPath { get; }

        ServiceProviderConfiguration ServiceProviderConfiguration { get; }

        string RequestId { get; }

        LoadBalancerOptions LoadBalancerOptions { get; }

        string DownstreamScheme { get; }

        QoSOptions QoSOptions { get; }

        HttpHandlerOptions HttpHandlerOptions { get; }
    }
}
