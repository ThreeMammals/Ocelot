namespace Ocelot.Configuration.File;

/// <summary>
/// Represents the JSON structure of a standard static route (no service discovery).
/// </summary>
public class FileRoute : FileRouteBase, IRouteUpstream, IRouteGrouping, IRouteRateLimiting, ICloneable
{
    public FileRoute()
    {
        AddClaimsToRequest = new Dictionary<string, string>();
        AddHeadersToRequest = new Dictionary<string, string>();
        AddQueriesToRequest = new Dictionary<string, string>();
        AuthenticationOptions = null;
        ChangeDownstreamPathTemplate = new Dictionary<string, string>();
        DangerousAcceptAnyServerCertificateValidator = false;
        DelegatingHandlers = new List<string>();
        DownstreamHeaderTransform = new Dictionary<string, string>();
        DownstreamHostAndPorts = new List<FileHostAndPort>();
        DownstreamHttpMethod = null;
        DownstreamHttpVersion = null;
        DownstreamHttpVersionPolicy = null;
        DownstreamPathTemplate = null;
        DownstreamScheme = null; // to be reviewed 
        CacheOptions = null;
        FileCacheOptions = null;
        HttpHandlerOptions = null;
        Key = null;
        LoadBalancerOptions = null;
        Metadata = null;
        Priority = 1; // to be reviewed WTF?
        QoSOptions = new FileQoSOptions();
        RateLimiting = null;
        RateLimitOptions = null;
        RequestIdKey = null;
        RouteClaimsRequirement = new Dictionary<string, string>();
        RouteIsCaseSensitive = false;
        SecurityOptions = new FileSecurityOptions();
        ServiceName = null;
        ServiceNamespace = null;
        Timeout = null;
        UpstreamHeaderTemplates = new Dictionary<string, string>();
        UpstreamHeaderTransform = new Dictionary<string, string>();
        UpstreamHost = null;
        UpstreamHttpMethod = new();
        UpstreamPathTemplate = null;
    }

    public FileRoute(FileRoute from)
    {
        DeepCopy(from, this);
    }

    public Dictionary<string, string> AddClaimsToRequest { get; set; }
    public Dictionary<string, string> AddHeadersToRequest { get; set; }
    public Dictionary<string, string> AddQueriesToRequest { get; set; }
    public Dictionary<string, string> ChangeDownstreamPathTemplate { get; set; }
    public bool DangerousAcceptAnyServerCertificateValidator { get; set; }
    public List<string> DelegatingHandlers { get; set; }
    public IDictionary<string, string> DownstreamHeaderTransform { get; set; }
    public List<FileHostAndPort> DownstreamHostAndPorts { get; set; }
    public string DownstreamHttpMethod { get; set; }
    public string DownstreamPathTemplate { get; set; }
    [Obsolete("Use CacheOptions instead of FileCacheOptions! Note that FileCacheOptions will be removed in version 25.0!")]
    public FileCacheOptions FileCacheOptions { get; set; }
    public int Priority { get; set; }
    public string RequestIdKey { get; set; }
    public Dictionary<string, string> RouteClaimsRequirement { get; set; }
    public bool RouteIsCaseSensitive { get; set; }
    public FileSecurityOptions SecurityOptions { get; set; }
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
        to.AuthenticationOptions = from.AuthenticationOptions is null ? null : new(from.AuthenticationOptions);
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
