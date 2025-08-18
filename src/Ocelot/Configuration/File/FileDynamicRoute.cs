using Ocelot.Configuration.Creator;

namespace Ocelot.Configuration.File;

/// <summary>
/// TODO: Make it as a base Route File-model.
/// </summary>
public class FileDynamicRoute : IRouteRateLimiting
{
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
    public IDictionary<string, string> Metadata { get; set; }
    public FileRateLimitRule RateLimitRule { get; set; }
    public string ServiceName { get; set; }

    public FileDynamicRoute()
    {
        DownstreamHttpVersion = default;
        DownstreamHttpVersionPolicy = default;
        Metadata = new Dictionary<string, string>();
        RateLimitRule = default;
        ServiceName = default;
    }

    /// <summary>The timeout in seconds for requests.</summary>
    /// <value>A <see cref="Nullable{T}"/> where T is <see cref="int"/> value in seconds.</value>
    public int? Timeout { get; set; }

    // IRouteRateLimiting vs IRouteUpstream
    public FileRateLimitRule RateLimitOptions => RateLimitRule;
    public IDictionary<string, string> UpstreamHeaderTemplates => new Dictionary<string, string>();
    public string UpstreamPathTemplate { get => ServiceName; }
    public List<string> UpstreamHttpMethod { get; set; }
    public bool RouteIsCaseSensitive => false;
    public int Priority => 0;
}
