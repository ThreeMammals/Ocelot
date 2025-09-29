#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable SA1133 // Do not combine attributes
#pragma warning disable SA1134 // Attributes should not share line

using Ocelot.Configuration.Creator;
using System.Text.Json.Serialization;
using NewtonsoftJsonIgnore = Newtonsoft.Json.JsonIgnoreAttribute;

namespace Ocelot.Configuration.File;

/// <summary>
/// Represents the JSON structure of a dynamic route in dynamic routing mode using service discovery.
/// </summary>
public class FileDynamicRoute : FileGlobalDynamicRoute, IRouteGrouping, IRouteRateLimiting
{
    public IDictionary<string, string> Metadata { get; set; }

    [Obsolete("Use RateLimitOptions instead of RateLimitRule! Note that RateLimitRule will be removed in version 25.0!")]
    public FileRateLimitByHeaderRule RateLimitRule { get; set; }
    [NewtonsoftJsonIgnore, JsonIgnore] public FileRateLimiting RateLimiting { get; set; } // publish the schema in version 25.0!

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
