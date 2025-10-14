using Ocelot.Configuration.File;

namespace Ocelot.Configuration;

public class InternalConfiguration : IInternalConfiguration
{
    public InternalConfiguration() => Routes = new();

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
        HttpVersionPolicy downstreamHttpVersionPolicy,
        MetadataOptions metadataOptions,
        RateLimitOptions rateLimitOptions,
        int? timeout)
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
        MetadataOptions = metadataOptions;
        RateLimitOptions = rateLimitOptions;
        Timeout = timeout;
    }

    public List<Route> Routes { get; init; }
    public string AdministrationPath { get; init; }
    public ServiceProviderConfiguration ServiceProviderConfiguration { get; init; }
    public string RequestId { get; init; }
    public LoadBalancerOptions LoadBalancerOptions { get; init; }
    public string DownstreamScheme { get; init; }
    public QoSOptions QoSOptions { get; init; }
    public HttpHandlerOptions HttpHandlerOptions { get; init; }

    /// <summary>Global HTTP version policy. It is related to <see cref="FileRoute.DownstreamHttpVersionPolicy"/> property.</summary>
    /// <value>An <see cref="HttpVersionPolicy"/> enumeration value.</value>
    public HttpVersionPolicy DownstreamHttpVersionPolicy { get; init; }
    public Version DownstreamHttpVersion { get; init; }
    public MetadataOptions MetadataOptions { get; init; }
    public RateLimitOptions RateLimitOptions { get; init; }
    public int? Timeout { get; init; }
}
