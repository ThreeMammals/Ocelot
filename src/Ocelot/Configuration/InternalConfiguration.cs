using Ocelot.Configuration.File;

namespace Ocelot.Configuration;

public class InternalConfiguration : IInternalConfiguration
{
    public InternalConfiguration() => Routes = new();
    public InternalConfiguration(List<Route> routes) => Routes = routes ?? new();

    public string AdministrationPath { get; init; }
    public CacheOptions CacheOptions { get; set; }
    public Version DownstreamHttpVersion { get; init; }

    /// <summary>Global HTTP version policy. It is related to <see cref="FileRoute.DownstreamHttpVersionPolicy"/> property.</summary>
    /// <value>An <see cref="HttpVersionPolicy"/> enumeration value.</value>
    public HttpVersionPolicy DownstreamHttpVersionPolicy { get; init; }
    public string DownstreamScheme { get; init; }
    public HttpHandlerOptions HttpHandlerOptions { get; init; }
    public LoadBalancerOptions LoadBalancerOptions { get; init; }
    public MetadataOptions MetadataOptions { get; init; }
    public QoSOptions QoSOptions { get; init; }
    public RateLimitOptions RateLimitOptions { get; init; }
    public string RequestId { get; init; }
    public List<Route> Routes { get; init; }
    public ServiceProviderConfiguration ServiceProviderConfiguration { get; init; }
    public int? Timeout { get; init; }
}
