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
public class FileDynamicRoute : IRouteGrouping, IRouteRateLimiting
{
    /// <summary>The <see cref="HttpVersionPolicy"/> enum specifies behaviors for selecting and negotiating the HTTP version for a request.</summary>
    /// <value>A <see langword="string" /> value of defined <see cref="VersionPolicies"/> constants.</value>
    /// <remarks>
    /// Related to the <see cref="DownstreamHttpVersion"/> property.
    /// <list type="bullet">
    ///   <item><see href="https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpversionpolicy">HttpVersionPolicy Enum</see></item>
    ///   <item><see href="https://learn.microsoft.com/en-us/dotnet/api/system.net.httpversion">HttpVersion Class</see></item>
    ///   <item><see href="https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httprequestmessage.versionpolicy">HttpRequestMessage.VersionPolicy Property</see></item>
    /// </list>
    /// </remarks>
    public string DownstreamHttpVersionPolicy { get; set; }
    public string DownstreamHttpVersion { get; set; }

    public IDictionary<string, string> Metadata { get; set; }

    [Obsolete("Use RateLimitOptions instead of RateLimitRule! Note that RateLimitRule will be removed in version 25.0!")]
    public FileRateLimitByHeaderRule RateLimitRule { get; set; }
    public FileRateLimitByHeaderRule RateLimitOptions { get; set; } // => RateLimitRule; // IRouteRateLimiting
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
    }

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

    // IRouteGrouping
    public string Key { get; set; }

    // IRouteUpstream
    public IDictionary<string, string> UpstreamHeaderTemplates { get; set; }

    //public string UpstreamPathTemplate { get; set; }
    public string UpstreamPathTemplate { get => ServiceName; }
    public HashSet<string> UpstreamHttpMethod { get; set; }

    public bool RouteIsCaseSensitive { get; set; }
    public int Priority => 0;
}
