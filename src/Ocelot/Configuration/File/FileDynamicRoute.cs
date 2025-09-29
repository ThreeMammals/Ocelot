namespace Ocelot.Configuration.File;

/// <summary>
/// Represents the JSON structure of a dynamic route in dynamic routing mode using service discovery.
/// </summary>
public class FileDynamicRoute : FileGlobalDynamicRoute, IRouteGrouping, IRouteRateLimiting
{
    [Obsolete("Use RateLimitOptions instead of RateLimitRule! Note that RateLimitRule will be removed in version 25.0!")]
    public FileRateLimitByHeaderRule RateLimitRule { get; set; }

    public string ServiceName { get; set; }
    public string ServiceNamespace { get; set; }

    public FileDynamicRoute()
    {
        DownstreamHttpVersion = default;
        DownstreamHttpVersionPolicy = default;
        Metadata = new Dictionary<string, string>();
        RateLimitRule = default;
        RateLimitOptions = default;
        RateLimiting = default;
        ServiceName = default;
        ServiceNamespace = default;
    }

    public string Key { get; set; } // IRouteGrouping
}
