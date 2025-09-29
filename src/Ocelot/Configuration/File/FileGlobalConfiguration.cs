#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable SA1133 // Do not combine attributes
#pragma warning disable SA1134 // Attributes should not share line

using System.Text.Json.Serialization;
using NewtonsoftJsonIgnore = Newtonsoft.Json.JsonIgnoreAttribute;

namespace Ocelot.Configuration.File;

public class FileGlobalConfiguration : FileGlobalDynamicRoute
{
    public FileGlobalConfiguration()
    {
        AuthenticationOptions = new();
        BaseUrl = default;
        CacheOptions = new();
        DownstreamHeaderTransform = new Dictionary<string, string>();
        DownstreamHttpVersion = default;
        DownstreamHttpVersionPolicy = default;
        DownstreamScheme = default;
        HttpHandlerOptions = new();
        LoadBalancerOptions = new();
        MetadataOptions = new();
        QoSOptions = new();
        RateLimitOptions = default;
        RateLimiting = default;
        RequestIdKey = default;
        SecurityOptions = new();
        ServiceDiscoveryProvider = new();
        Timeout = null;
        UpstreamHeaderTransform = new Dictionary<string, string>();
    }

    public FileAuthenticationOptions AuthenticationOptions { get; set; }
    public string BaseUrl { get; set; }
    public FileCacheOptions CacheOptions { get; set; }
    public IDictionary<string, string> DownstreamHeaderTransform { get; set; }
    public FileHttpHandlerOptions HttpHandlerOptions { get; set; }
    public FileLoadBalancerOptions LoadBalancerOptions { get; set; }
    public FileMetadataOptions MetadataOptions { get; set; }
    public FileQoSOptions QoSOptions { get; set; }
    public new FileGlobalRateLimitByHeaderRule RateLimitOptions { get; set; }
    [NewtonsoftJsonIgnore, JsonIgnore] public FileGlobalRateLimiting RateLimiting { get; set; } // publish the schema in version 25.0!
    public string RequestIdKey { get; set; }
    public FileSecurityOptions SecurityOptions { get; set; }
    public FileServiceDiscoveryProvider ServiceDiscoveryProvider { get; set; }
    public IDictionary<string, string> UpstreamHeaderTransform { get; set; }
}
