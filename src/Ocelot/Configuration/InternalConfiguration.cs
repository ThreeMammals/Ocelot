using Ocelot.Configuration.File;

namespace Ocelot.Configuration;

public class InternalConfiguration : IInternalConfiguration
{
    public InternalConfiguration(
        List<Route> routes,
        string administrationPath,
        ServiceProviderConfiguration serviceProviderConfiguration,
        string requestId,
        LoadBalancerOptions loadBalancerOptions,
        string downstreamScheme,
        QoSOptions qoSOptions,
        HttpHandlerOptions httpHandlerOptions,
        Version downstreamHttpVersion,
        HttpVersionPolicy? downstreamHttpVersionPolicy)
    {
        Routes = routes;
        AdministrationPath = administrationPath;
        ServiceProviderConfiguration = serviceProviderConfiguration;
        RequestId = requestId;
        LoadBalancerOptions = loadBalancerOptions;
        DownstreamScheme = downstreamScheme;
        QoSOptions = qoSOptions;
        HttpHandlerOptions = httpHandlerOptions;
        DownstreamHttpVersion = downstreamHttpVersion;
        DownstreamHttpVersionPolicy = downstreamHttpVersionPolicy;
    }

    public List<Route> Routes { get; }
    public string AdministrationPath { get; }
    public ServiceProviderConfiguration ServiceProviderConfiguration { get; }
    public string RequestId { get; }
    public LoadBalancerOptions LoadBalancerOptions { get; }
    public string DownstreamScheme { get; }
    public QoSOptions QoSOptions { get; }
    public HttpHandlerOptions HttpHandlerOptions { get; }

    public Version DownstreamHttpVersion { get; }

    /// <summary>Global HTTP version policy. It is related to <see cref="FileRoute.DownstreamHttpVersionPolicy"/> property.</summary>
    /// <value>An <see cref="HttpVersionPolicy"/> enumeration value.</value>
    public HttpVersionPolicy? DownstreamHttpVersionPolicy { get; }
}
