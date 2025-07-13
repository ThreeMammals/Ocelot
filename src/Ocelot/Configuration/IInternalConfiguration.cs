using Ocelot.Configuration.File;

namespace Ocelot.Configuration;

public interface IInternalConfiguration
{
    List<Route> Routes { get; }

    string AdministrationPath { get; }

    ServiceProviderConfiguration ServiceProviderConfiguration { get; }

    string RequestId { get; }

    LoadBalancerOptions LoadBalancerOptions { get; }

    string DownstreamScheme { get; }

    QoSOptions QoSOptions { get; }

    HttpHandlerOptions HttpHandlerOptions { get; }

    Version DownstreamHttpVersion { get; }

    /// <summary>Global HTTP version policy. It is related to <see cref="FileRoute.DownstreamHttpVersionPolicy"/> property.</summary>
    /// <value>An <see cref="HttpVersionPolicy"/> enumeration value.</value>
    HttpVersionPolicy? DownstreamHttpVersionPolicy { get; }
}
