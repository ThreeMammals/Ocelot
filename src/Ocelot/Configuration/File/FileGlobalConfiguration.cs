namespace Ocelot.Configuration.File;

public class FileGlobalConfiguration
{
    public FileGlobalConfiguration()
    {
        AuthenticationOptions = new();
        BaseUrl = default;
        CacheOptions = default;
        DownstreamHeaderTransform = new Dictionary<string, string>();
        DownstreamHttpVersion = default;
        DownstreamHttpVersionPolicy = default;
        DownstreamScheme = default;
        HttpHandlerOptions = new();
        LoadBalancerOptions = default;
        Metadata = default;
        MetadataOptions = new();
        QoSOptions = new();
        RateLimitOptions = default;
        RequestIdKey = default;
        SecurityOptions = new();
        ServiceDiscoveryProvider = new();
        Timeout = null;
        UpstreamHeaderTransform = new Dictionary<string, string>();
    }

    public FileGlobalAuthenticationOptions AuthenticationOptions { get; set; }
    public string BaseUrl { get; set; }
    public FileGlobalCacheOptions CacheOptions { get; set; }
    public IDictionary<string, string> DownstreamHeaderTransform { get; set; }
    public string DownstreamHttpVersion { get; set; }
    public string DownstreamHttpVersionPolicy { get; set; }
    public string DownstreamScheme { get; set; }
    public FileGlobalHttpHandlerOptions HttpHandlerOptions { get; set; }
    public FileGlobalLoadBalancerOptions LoadBalancerOptions { get; set; }
    public IDictionary<string, string> Metadata { get; set; }
    public FileMetadataOptions MetadataOptions { get; set; }
    public FileGlobalQoSOptions QoSOptions { get; set; }
    public FileGlobalRateLimitByHeaderRule RateLimitOptions { get; set; }
    public string RequestIdKey { get; set; }
    public FileSecurityOptions SecurityOptions { get; set; }
    public FileServiceDiscoveryProvider ServiceDiscoveryProvider { get; set; }

    /// <summary>Explicit timeout value which overrides default <see cref="DownstreamRoute.DefaultTimeoutSeconds"/>.</summary>
    /// <remarks>Notes:
    /// <list type="bullet">
    ///   <item><see cref="DownstreamRoute.Timeout"/> is the consumer of this property.</item>
    ///   <item><see cref="DownstreamRoute.DefaultTimeoutSeconds"/> implicitly overrides this property if not defined (null).</item>
    ///   <item><see cref="QoSOptions.TimeoutValue"/> explicitly overrides this property if QoS is enabled.</item>
    /// </list>
    /// </remarks>
    /// <value>A <see cref="Nullable{T}"/> (T is <see cref="int"/>) value, in seconds.</value>
    public int? Timeout { get; set; }
    public IDictionary<string, string> UpstreamHeaderTransform { get; set; }
}
