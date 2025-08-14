using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Values;

namespace Ocelot.Configuration;

public class DownstreamRoute
{
    public DownstreamRoute(
        string key,
        UpstreamPathTemplate upstreamPathTemplate,
        List<HeaderFindAndReplace> upstreamHeadersFindAndReplace,
        List<HeaderFindAndReplace> downstreamHeadersFindAndReplace,
        List<DownstreamHostAndPort> downstreamAddresses,
        string serviceName,
        string serviceNamespace,
        HttpHandlerOptions httpHandlerOptions,
        bool useServiceDiscovery,
        bool enableEndpointEndpointRateLimiting,
        QoSOptions qosOptions,
        string downstreamScheme,
        string requestIdKey,
        bool isCached,
        CacheOptions cacheOptions,
        LoadBalancerOptions loadBalancerOptions,
        RateLimitOptions rateLimitOptions,
        IEnumerable<GlobalRateLimitOptions> globalRateLimitOption,
        Dictionary<string, string> routeClaimsRequirement,
        List<ClaimToThing> claimsToQueries,
        List<ClaimToThing> claimsToHeaders,
        List<ClaimToThing> claimsToClaims,
        List<ClaimToThing> claimsToPath,
        bool isAuthenticated,
        bool isAuthorized,
        AuthenticationOptions authenticationOptions,
        DownstreamPathTemplate downstreamPathTemplate,
        string loadBalancerKey,
        List<string> delegatingHandlers,
        List<AddHeader> addHeadersToDownstream,
        List<AddHeader> addHeadersToUpstream,
        bool dangerousAcceptAnyServerCertificateValidator,
        SecurityOptions securityOptions,
        string downstreamHttpMethod,
        Version downstreamHttpVersion,
        HttpVersionPolicy downstreamHttpVersionPolicy,
        Dictionary<string, UpstreamHeaderTemplate> upstreamHeaders,
        MetadataOptions metadataOptions,
        int? timeout)
    {
        DangerousAcceptAnyServerCertificateValidator = dangerousAcceptAnyServerCertificateValidator;
        AddHeadersToDownstream = addHeadersToDownstream;
        DelegatingHandlers = delegatingHandlers;
        Key = key;
        UpstreamPathTemplate = upstreamPathTemplate;
        UpstreamHeadersFindAndReplace = upstreamHeadersFindAndReplace ?? new List<HeaderFindAndReplace>();
        DownstreamHeadersFindAndReplace = downstreamHeadersFindAndReplace ?? new List<HeaderFindAndReplace>();
        DownstreamAddresses = downstreamAddresses ?? new List<DownstreamHostAndPort>();
        ServiceName = serviceName;
        ServiceNamespace = serviceNamespace;
        HttpHandlerOptions = httpHandlerOptions;
        UseServiceDiscovery = useServiceDiscovery;
        EnableEndpointEndpointRateLimiting = enableEndpointEndpointRateLimiting;
        QosOptions = qosOptions;
        DownstreamScheme = downstreamScheme;
        RequestIdKey = requestIdKey;
        IsCached = isCached;
        CacheOptions = cacheOptions;
        LoadBalancerOptions = loadBalancerOptions;
        RateLimitOptions = rateLimitOptions;
        RouteClaimsRequirement = routeClaimsRequirement;
        ClaimsToQueries = claimsToQueries ?? new List<ClaimToThing>();
        ClaimsToHeaders = claimsToHeaders ?? new List<ClaimToThing>();
        ClaimsToClaims = claimsToClaims ?? new List<ClaimToThing>();
        ClaimsToPath = claimsToPath ?? new List<ClaimToThing>();
        IsAuthenticated = isAuthenticated;
        IsAuthorized = isAuthorized;
        AuthenticationOptions = authenticationOptions;
        DownstreamPathTemplate = downstreamPathTemplate;
        LoadBalancerKey = loadBalancerKey;
        AddHeadersToUpstream = addHeadersToUpstream;
        SecurityOptions = securityOptions;
        DownstreamHttpMethod = downstreamHttpMethod;
        DownstreamHttpVersion = downstreamHttpVersion;
        DownstreamHttpVersionPolicy = downstreamHttpVersionPolicy;
        UpstreamHeaders = upstreamHeaders ?? new();
        MetadataOptions = metadataOptions;
        Timeout = timeout;

        string path = UpstreamPathTemplate?.OriginalValue ?? string.Empty;
        string method = DownstreamHttpMethod ?? HttpMethods.Get;
        var globalRateLimit = globalRateLimitOption?.FirstOrDefault(g => g.Pattern.IsMatch(path) && g.Methods.Contains(method));

        if (globalRateLimit != null && !RateLimitOptions.EnableRateLimiting)
        {
            EnableEndpointEndpointRateLimiting = true;
            RateLimitOptions = new RateLimitOptionsBuilder()
                .WithDisableRateLimitHeaders(globalRateLimit.DisableRateLimitHeaders)
                .WithEnableRateLimiting(globalRateLimit.EnableRateLimiting)
                .WithHttpStatusCode(globalRateLimit.HttpStatusCode)
                .WithQuotaExceededMessage(globalRateLimit.QuotaExceededMessage)
                .WithRateLimitRule(new RateLimitRule(globalRateLimit.Period, ParsePeriodTimespan(globalRateLimit.Period), globalRateLimit.Limit))
                .WithClientWhiteList(() => [])
                .Build();
        }
    }

    private static double ParsePeriodTimespan(string period)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(period, nameof(period));

        char unit = period[^1]; // separate latest character
        string numberPart = period[..^1]; // all characters except latest
        if (!double.TryParse(numberPart, out var value))
        {
            throw new ArgumentException($"Invalid period number: {period}", nameof(period));
        }

        return unit switch
        {
            's' => value, // seconds
            'm' => value * 60, // minutes
            'h' => value * 3600, // hour
            'd' => value * 86400, // day
            _ => throw new ArgumentException($"Invalid period unit: {unit}", nameof(period)),
        };
    }

    public string Key { get; }
    public UpstreamPathTemplate UpstreamPathTemplate { get; }
    public List<HeaderFindAndReplace> UpstreamHeadersFindAndReplace { get; }
    public List<HeaderFindAndReplace> DownstreamHeadersFindAndReplace { get; }
    public List<DownstreamHostAndPort> DownstreamAddresses { get; }
    public string ServiceName { get; }
    public string ServiceNamespace { get; }
    public HttpHandlerOptions HttpHandlerOptions { get; }
    public bool EnableEndpointEndpointRateLimiting { get; } // TODO Naming mistyping
    public QoSOptions QosOptions { get; }
    public string DownstreamScheme { get; }
    public string RequestIdKey { get; }
    public bool IsCached { get; }
    public CacheOptions CacheOptions { get; }
    public LoadBalancerOptions LoadBalancerOptions { get; }
    public RateLimitOptions RateLimitOptions { get; }
    public Dictionary<string, string> RouteClaimsRequirement { get; }
    public List<ClaimToThing> ClaimsToQueries { get; }
    public List<ClaimToThing> ClaimsToHeaders { get; }
    public List<ClaimToThing> ClaimsToClaims { get; }
    public List<ClaimToThing> ClaimsToPath { get; }
    public bool IsAuthenticated { get; }
    public bool IsAuthorized { get; }
    public AuthenticationOptions AuthenticationOptions { get; }
    public DownstreamPathTemplate DownstreamPathTemplate { get; }
    public string LoadBalancerKey { get; }
    public List<string> DelegatingHandlers { get; }
    public List<AddHeader> AddHeadersToDownstream { get; }
    public List<AddHeader> AddHeadersToUpstream { get; }
    public bool DangerousAcceptAnyServerCertificateValidator { get; }
    public SecurityOptions SecurityOptions { get; }
    public string DownstreamHttpMethod { get; }
    public Version DownstreamHttpVersion { get; }

    /// <summary>The <see cref="HttpVersionPolicy"/> enum specifies behaviors for selecting and negotiating the HTTP version for a request.</summary>
    /// <value>An <see cref="HttpVersionPolicy"/> enum value being mapped from a <see cref="VersionPolicies"/> constant.</value>
    /// <remarks>
    /// Related to the <see cref="DownstreamHttpVersion"/> property.
    /// <list type="bullet">
    ///   <item><see href="https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpversionpolicy">HttpVersionPolicy Enum</see></item>
    ///   <item><see href="https://learn.microsoft.com/en-us/dotnet/api/system.net.httpversion">HttpVersion Class</see></item>
    ///   <item><see href="https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httprequestmessage.versionpolicy">HttpRequestMessage.VersionPolicy Property</see></item>
    /// </list>
    /// </remarks>
    public HttpVersionPolicy DownstreamHttpVersionPolicy { get; }
    public Dictionary<string, UpstreamHeaderTemplate> UpstreamHeaders { get; }
    public bool UseServiceDiscovery { get; }
    public MetadataOptions MetadataOptions { get; }

    /// <summary>The timeout duration for the downstream request in seconds.</summary>
    /// <value>A <see cref="Nullable{T}"/> (T is <see cref="int"/>) value, in seconds.</value>
    public int? Timeout { get; }
    public const int LowTimeout = 3;  //  3 seconds
    public const int DefTimeout = 90; // 90 seconds

    /// <summary>Gets or sets the default timeout in seconds for all routes, applicable at both the route-level and globally.
    /// <para>The setter includes a constraint that ensures the assigned value is greater than or equal to <see cref="LowTimeout"/> (3 seconds).</para></summary>
    /// <remarks>By default, initialized to <see cref="DefTimeout"/> (90 seconds).</remarks>
    /// <value>An <see cref="int"/> value in seconds.</value>
    public static int DefaultTimeoutSeconds { get => defaultTimeoutSeconds; set => defaultTimeoutSeconds = value >= LowTimeout ? value : DefTimeout; }
    private static int defaultTimeoutSeconds = DefTimeout;

    /// <summary>Gets the route name depending on whether the service discovery mode is enabled or disabled.</summary>
    /// <returns>A <see cref="string"/> object with the name.</returns>
    public string Name()
    {
        var path = !string.IsNullOrEmpty(UpstreamPathTemplate?.OriginalValue)
            ? UpstreamPathTemplate.OriginalValue
            : !string.IsNullOrEmpty(DownstreamPathTemplate.Value) // can't be null because it is created by DownstreamRouteBuilder
                ? DownstreamPathTemplate.ToString()
                : "?";
        return UseServiceDiscovery || !string.IsNullOrEmpty(ServiceName)
            ? string.Join(':', ServiceNamespace, ServiceName, path)
            : path;
    }
}
