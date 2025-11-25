#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable SA1133 // Do not combine attributes

using Ocelot.Configuration.Creator;
using System.Text.Json.Serialization;
using NewtonsoftJsonIgnore = Newtonsoft.Json.JsonIgnoreAttribute;

namespace Ocelot.Configuration.File;

/// <summary>
/// Defines common aggregation for dynamic and static routes.
/// </summary>
public abstract class FileRouteBase : IRouteGrouping
{
    public FileAuthenticationOptions AuthenticationOptions { get; set; }
    public FileCacheOptions CacheOptions { get; set; }

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
    public string DownstreamScheme { get; set; }
    public FileHttpHandlerOptions HttpHandlerOptions { get; set; }
    public string Key { get; set; } // IRouteGrouping
    public FileLoadBalancerOptions LoadBalancerOptions { get; set; }
    public IDictionary<string, string> Metadata { get; set; }
    public FileQoSOptions QoSOptions { get; set; }
    public FileRateLimitByHeaderRule RateLimitOptions { get; set; } // IRouteRateLimiting
    [NewtonsoftJsonIgnore, JsonIgnore] // publish the schema in version 25.1!
    public FileRateLimiting RateLimiting { get; set; }
    public string ServiceName { get; set; }
    public string ServiceNamespace { get; set; }

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
}
