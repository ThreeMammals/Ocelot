#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable SA1133 // Do not combine attributes
#pragma warning disable SA1134 // Attributes should not share line

using Ocelot.Configuration.Creator;
using System.Text.Json.Serialization;
using NewtonsoftJsonIgnore = Newtonsoft.Json.JsonIgnoreAttribute;

namespace Ocelot.Configuration.File;

/// <summary>
/// Represents the JSON structure of a standard static route (no service discovery).
/// </summary>
public class FileRoute : IRouteUpstream, IRouteGrouping, IRouteRateLimiting, ICloneable
{
    public FileRoute()
    {
        AddClaimsToRequest = new Dictionary<string, string>();
        AddHeadersToRequest = new Dictionary<string, string>();
        AddQueriesToRequest = new Dictionary<string, string>();
        AuthenticationOptions = new FileAuthenticationOptions();
        ChangeDownstreamPathTemplate = new Dictionary<string, string>();
        DangerousAcceptAnyServerCertificateValidator = false;
        DelegatingHandlers = new List<string>();
        DownstreamHeaderTransform = new Dictionary<string, string>();
        DownstreamHostAndPorts = new List<FileHostAndPort>();
        DownstreamHttpMethod = default;
        DownstreamHttpVersion = default;
        DownstreamHttpVersionPolicy = default;
        DownstreamPathTemplate = default;
        DownstreamScheme = default; // to be reviewed 
        CacheOptions = default;
        FileCacheOptions = default;
        HttpHandlerOptions = default;
        Key = default;
        LoadBalancerOptions = default;
        Metadata = default;
        Priority = 1; // to be reviewed WTF?
        QoSOptions = new FileQoSOptions();
        RateLimiting = default;
        RateLimitOptions = default;
        RequestIdKey = default;
        RouteClaimsRequirement = new Dictionary<string, string>();
        RouteIsCaseSensitive = default;
        SecurityOptions = new FileSecurityOptions();
        ServiceName = default;
        ServiceNamespace = default;
        Timeout = default;
        UpstreamHeaderTemplates = new Dictionary<string, string>();
        UpstreamHeaderTransform = new Dictionary<string, string>();
        UpstreamHost = default;
        UpstreamHttpMethod = new();
        UpstreamPathTemplate = default;
    }

    public FileRoute(FileRoute from)
    {
        DeepCopy(from, this);
    }

    public Dictionary<string, string> AddClaimsToRequest { get; set; }
    public Dictionary<string, string> AddHeadersToRequest { get; set; }
    public Dictionary<string, string> AddQueriesToRequest { get; set; }
    public FileAuthenticationOptions AuthenticationOptions { get; set; }
    public Dictionary<string, string> ChangeDownstreamPathTemplate { get; set; }
    public bool DangerousAcceptAnyServerCertificateValidator { get; set; }
    public List<string> DelegatingHandlers { get; set; }
    public IDictionary<string, string> DownstreamHeaderTransform { get; set; }
    public List<FileHostAndPort> DownstreamHostAndPorts { get; set; }
    public string DownstreamHttpMethod { get; set; }
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
    public string DownstreamPathTemplate { get; set; }
    public string DownstreamScheme { get; set; }
    public FileCacheOptions CacheOptions { get; set; }
    [Obsolete("Use CacheOptions instead of FileCacheOptions! Note that FileCacheOptions will be removed in version 25.0!")]
    public FileCacheOptions FileCacheOptions { get; set; }
    public FileHttpHandlerOptions HttpHandlerOptions { get; set; }
    public string Key { get; set; } // IRouteGrouping
    public FileLoadBalancerOptions LoadBalancerOptions { get; set; }
    public IDictionary<string, string> Metadata { get; set; }
    public int Priority { get; set; }
    public FileQoSOptions QoSOptions { get; set; }
    public FileRateLimitByHeaderRule RateLimitOptions { get; set; }
    [NewtonsoftJsonIgnore, JsonIgnore] public FileRateLimiting RateLimiting { get; set; } // publish the schema in version 25.0!
    public string RequestIdKey { get; set; }
    public Dictionary<string, string> RouteClaimsRequirement { get; set; }
    public bool RouteIsCaseSensitive { get; set; }
    public FileSecurityOptions SecurityOptions { get; set; }
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
    public IDictionary<string, string> UpstreamHeaderTemplates { get; set; }
    public IDictionary<string, string> UpstreamHeaderTransform { get; set; }
    public string UpstreamHost { get; set; }
    public HashSet<string> UpstreamHttpMethod { get; set; }
    public string UpstreamPathTemplate { get; set; }

    /// <summary>
    /// Clones this object by making a deep copy.
    /// </summary>
    /// <returns>A <see cref="FileRoute"/> deeply copied object.</returns>
    public object Clone()
    {
        var other = (FileRoute)MemberwiseClone();
        DeepCopy(this, other);
        return other;
    }

    public static void DeepCopy(FileRoute from, FileRoute to)
    {
        to.AddClaimsToRequest = new(from.AddClaimsToRequest);
        to.AddHeadersToRequest = new(from.AddHeadersToRequest);
        to.AddQueriesToRequest = new(from.AddQueriesToRequest);
        to.AuthenticationOptions = new(from.AuthenticationOptions);
        to.ChangeDownstreamPathTemplate = new(from.ChangeDownstreamPathTemplate);
        to.DangerousAcceptAnyServerCertificateValidator = from.DangerousAcceptAnyServerCertificateValidator;
        to.DelegatingHandlers = new(from.DelegatingHandlers);
        to.DownstreamHeaderTransform = new Dictionary<string, string>(from.DownstreamHeaderTransform);
        to.DownstreamHostAndPorts = from.DownstreamHostAndPorts.Select(x => new FileHostAndPort(x)).ToList();
        to.DownstreamHttpMethod = from.DownstreamHttpMethod;
        to.DownstreamHttpVersion = from.DownstreamHttpVersion;
        to.DownstreamHttpVersionPolicy = from.DownstreamHttpVersionPolicy;
        to.DownstreamPathTemplate = from.DownstreamPathTemplate;
        to.DownstreamScheme = from.DownstreamScheme;
        to.CacheOptions = new(from.CacheOptions);
        to.FileCacheOptions = new(from.FileCacheOptions);
        to.HttpHandlerOptions = new(from.HttpHandlerOptions);
        to.Key = from.Key;
        to.LoadBalancerOptions = new(from.LoadBalancerOptions);
        to.Metadata = new Dictionary<string, string>(from.Metadata);
        to.Priority = from.Priority;
        to.QoSOptions = new(from.QoSOptions);
        to.RateLimiting = from.RateLimiting; // new(from.RateLimiting)
        to.RateLimitOptions = new(from.RateLimitOptions);
        to.RequestIdKey = from.RequestIdKey;
        to.RouteClaimsRequirement = new(from.RouteClaimsRequirement);
        to.RouteIsCaseSensitive = from.RouteIsCaseSensitive;
        to.SecurityOptions = new(from.SecurityOptions);
        to.ServiceName = from.ServiceName;
        to.ServiceNamespace = from.ServiceNamespace;
        to.Timeout = from.Timeout;
        to.UpstreamHeaderTemplates = new Dictionary<string, string>(from.UpstreamHeaderTemplates);
        to.UpstreamHeaderTransform = new Dictionary<string, string>(from.UpstreamHeaderTransform);
        to.UpstreamHost = from.UpstreamHost;
        to.UpstreamHttpMethod = new(from.UpstreamHttpMethod);
        to.UpstreamPathTemplate = from.UpstreamPathTemplate;
    }

    public override string ToString()
    {
        if (!string.IsNullOrWhiteSpace(Key))
        {
            return Key;
        }

        var path = !string.IsNullOrEmpty(UpstreamPathTemplate) ? UpstreamPathTemplate
            : !string.IsNullOrEmpty(DownstreamPathTemplate) ? DownstreamPathTemplate
            : "?";
        return !string.IsNullOrWhiteSpace(ServiceName)
            ? string.Join(':', ServiceNamespace, ServiceName, path)
            : path;
    }
}
