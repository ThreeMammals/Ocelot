#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable SA1133 // Do not combine attributes
#pragma warning disable SA1134 // Attributes should not share line

using Ocelot.Configuration.Creator;
using System.Text.Json.Serialization;
using NewtonsoftJsonIgnore = Newtonsoft.Json.JsonIgnoreAttribute;

namespace Ocelot.Configuration.File;

public class FileGlobalConfiguration
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
    public string DownstreamHttpVersion { get; set; }

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
    public string DownstreamScheme { get; set; }
    public FileHttpHandlerOptions HttpHandlerOptions { get; set; }
    public FileLoadBalancerOptions LoadBalancerOptions { get; set; }
    public FileMetadataOptions MetadataOptions { get; set; }
    public FileQoSOptions QoSOptions { get; set; }
    public FileGlobalRateLimitByHeaderRule RateLimitOptions { get; set; }
    [NewtonsoftJsonIgnore, JsonIgnore] public FileGlobalRateLimiting RateLimiting { get; set; } // publish the schema in version 25.0!
    public string RequestIdKey { get; set; }
    public FileSecurityOptions SecurityOptions { get; set; }
    public FileServiceDiscoveryProvider ServiceDiscoveryProvider { get; set; }

    /// <summary>The timeout in seconds for requests.</summary>
    /// <value>A <see cref="Nullable{T}"/> (T is <see cref="int"/>) value in seconds.</value>
    public int? Timeout { get; set; }
    public IDictionary<string, string> UpstreamHeaderTransform { get; set; }
}
