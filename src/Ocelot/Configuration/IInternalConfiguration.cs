namespace Ocelot.Configuration;

public interface IInternalConfiguration
{
    string AdministrationPath { get; }
    CacheOptions CacheOptions { get; }
    Version DownstreamHttpVersion { get; }
    HttpVersionPolicy DownstreamHttpVersionPolicy { get; }
    string DownstreamScheme { get; }
    HttpHandlerOptions HttpHandlerOptions { get; }
    LoadBalancerOptions LoadBalancerOptions { get; }
    MetadataOptions MetadataOptions { get; }
    QoSOptions QoSOptions { get; }
    RateLimitOptions RateLimitOptions { get; }
    string RequestId { get; }
    List<Route> Routes { get; }
    ServiceProviderConfiguration ServiceProviderConfiguration { get; }
    int? Timeout { get; }
}
