using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Responses;
using Ocelot.Values;

namespace Ocelot.DownstreamRouteFinder.Finder;

/// <summary>
/// Dynamic route finder when service discovery is enabled.
/// </summary>
public class DiscoveryDownstreamRouteFinder : IDownstreamRouteProvider
{
    public const char Dot = '.';
    public const char Slash = '/';
    public const char Question = '?';

    private readonly ConcurrentDictionary<string, Route> _cache;
    private readonly IRouteKeyCreator _routeKeyCreator;
    private readonly IUpstreamHeaderTemplatePatternCreator _upstreamHeaderTemplatePatternCreator;

    public DiscoveryDownstreamRouteFinder(
        IRouteKeyCreator routeKeyCreator,
        IUpstreamHeaderTemplatePatternCreator upstreamHeaderTemplatePatternCreator)
    {
        _cache = new();
        _routeKeyCreator = routeKeyCreator;
        _upstreamHeaderTemplatePatternCreator = upstreamHeaderTemplatePatternCreator;
    }

    public Response<Route> Get(string upstreamUrlPath, string upstreamQueryString, string upstreamHttpMethod,
        IInternalConfiguration configuration, string upstreamHost, IHeaderDictionary upstreamHeaders)
    {
        var serviceName = GetServiceName(configuration, upstreamHeaders, upstreamHost, upstreamHttpMethod, upstreamUrlPath, upstreamQueryString,
            out var serviceNamespace);

        var downstreamPath = GetDownstreamPath(upstreamUrlPath);
        var dynamicRoute = configuration.Routes
            .Where(r => r.IsDynamic) // process dynamic routes only
            .SelectMany(r => r.DownstreamRoute)
            .FirstOrDefault(dr => dr.ServiceName == serviceName && (serviceNamespace.IsEmpty() || dr.ServiceNamespace == serviceNamespace));
        var loadBalancerKey = dynamicRoute != null
            ? dynamicRoute.LoadBalancerKey
            : _routeKeyCreator.Create(serviceNamespace, serviceName, configuration.LoadBalancerOptions);
        if (_cache.TryGetValue(loadBalancerKey, out var cachedRoute))
            return new OkResponse<Route>(cachedRoute);

        // TODO: Could it be that the static route functionality was possibly lost here? -> StaticRoutesCreator.SetUpRoute -> _upstreamTemplatePatternCreator
        var upstreamPathTemplate = new UpstreamPathTemplateBuilder().WithOriginalValue(upstreamUrlPath).Build();

        // TODO Under development. Implementation returns empty collection.
        var upstreamHeaderTemplates = _upstreamHeaderTemplatePatternCreator.Create(upstreamHeaders, false); // ? discoveryDownstreamRoute.UpstreamHeaders

        var routeBuilder = new DownstreamRouteBuilder()
            .WithServiceName(serviceName)
            .WithServiceNamespace(serviceNamespace)
            .WithAuthenticationOptions(configuration.AuthenticationOptions)
            .WithCacheOptions(configuration.CacheOptions)
            .WithDownstreamHttpVersion(configuration.DownstreamHttpVersion)
            .WithDownstreamHttpVersionPolicy(configuration.DownstreamHttpVersionPolicy)
            .WithDownstreamPathTemplate(downstreamPath)
            .WithDownstreamScheme(configuration.DownstreamScheme)
            .WithHttpHandlerOptions(configuration.HttpHandlerOptions)
            .WithLoadBalancerKey(loadBalancerKey)
            .WithLoadBalancerOptions(configuration.LoadBalancerOptions)
            .WithMetadata(configuration.MetadataOptions)
            .WithQosOptions(configuration.QoSOptions)
            .WithRateLimitOptions(configuration.RateLimitOptions)
            .WithUpstreamHeaders(upstreamHeaderTemplates as Dictionary<string, UpstreamHeaderTemplate>)
            .WithUpstreamPathTemplate(upstreamPathTemplate)
            .WithTimeout(configuration.Timeout);
        if (dynamicRoute != null)
        {
            // We are set to replace IInternalConfiguration global options with the current options from actual dynamic route
            routeBuilder
                .WithAuthenticationOptions(dynamicRoute.AuthenticationOptions)
                .WithCacheOptions(dynamicRoute.CacheOptions)
                .WithDownstreamHttpVersion(dynamicRoute.DownstreamHttpVersion)
                .WithDownstreamHttpVersionPolicy(dynamicRoute.DownstreamHttpVersionPolicy)
                .WithDownstreamScheme(dynamicRoute.DownstreamScheme)
                .WithHttpHandlerOptions(dynamicRoute.HttpHandlerOptions)
                .WithLoadBalancerKey(loadBalancerKey/*dynamicRoute.LoadBalancerKey*/)
                .WithLoadBalancerOptions(dynamicRoute.LoadBalancerOptions)
                .WithMetadata(dynamicRoute.MetadataOptions)
                .WithQosOptions(dynamicRoute.QosOptions)
                .WithRateLimitOptions(dynamicRoute.RateLimitOptions)
                .WithServiceName(serviceName/*dynamicRoute.ServiceName*/)
                .WithServiceNamespace(serviceNamespace/*dynamicRoute.ServiceNamespace*/)
                .WithTimeout(dynamicRoute.Timeout);
        }

        var downstreamRoute = routeBuilder.Build();
        var route = new Route(true, downstreamRoute) // IsDynamic -> true
        {
            UpstreamHeaderTemplates = upstreamHeaderTemplates,
            UpstreamHost = upstreamHost,
            UpstreamHttpMethod = [new(upstreamHttpMethod.Trim())],
            UpstreamTemplatePattern = upstreamPathTemplate,
        };
        var cached = _cache.GetOrAdd(loadBalancerKey, route);
        return new OkResponse<Route>(cached);
    }

    /// <summary>
    /// Gets the downstream path part of the URL without the service name.
    /// Thus, the first segment of the upstream URL is removed.
    /// </summary>
    /// <param name="upstreamUrlPath">The URL with the service name in the first segment.</param>
    /// <returns>A <see cref="string"/> object containing the downstream path.</returns>
    internal static string GetDownstreamPath(string upstreamUrlPath)
    {
        int index = upstreamUrlPath.IndexOf(Slash, 1);
        return index != -1
            ? upstreamUrlPath[index..]
            : Slash.ToString();
    }

    /// <summary>Gets service name and its namespace of request URL.
    /// <para>Note: A namespace and service name should be separated by a '.' (dot) character.</para></summary>
    /// <remarks>Example: <c>http://ocelot.net/namespace.service-name/path</c> URL.</remarks>
    /// <param name="upstreamUrlPath">The upstream path.</param>
    /// <param name="serviceNamespace">Extracted namespace.</param>
    /// <returns>A <see cref="string"/> object.</returns>
    protected virtual string GetServiceName(string upstreamUrlPath, out string serviceNamespace)
    {
        ReadOnlySpan<char> path = upstreamUrlPath.IsNotEmpty()
            ? upstreamUrlPath.AsSpan()
            : [Slash];
        int index = path[1..].IndexOf(Slash);
        ReadOnlySpan<char> name = index == -1
            ? path[1..]
            : path.Slice(1, index).TrimEnd(Slash);

        index = name.IndexOf(Dot);
        serviceNamespace = string.Empty;
        if (index == -1)
            return name.ToString();

        serviceNamespace = name[..index].ToString();
        return name[++index..].ToString();
    }

    protected virtual string GetServiceName(IInternalConfiguration configuration, IHeaderDictionary upstreamHeaders,
        string upstreamHost, string upstreamMethod, string upstreamUrlPath, string upstreamQueryString, out string serviceNamespace)
    {
        return GetServiceName(upstreamUrlPath, out serviceNamespace);
    }

    // protected string GetServiceName(HttpRequest request, out string serviceNamespace) => string.Empty;
}
