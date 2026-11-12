using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Errors;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Middleware;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery;
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
    private readonly IUpstreamHeaderTemplatePatternCreator _headerTemplatePatternCreator;
    private readonly IDynamicRouting _routing;

    public DiscoveryDownstreamRouteFinder(
        IRouteKeyCreator routeKeyCreator,
        IUpstreamHeaderTemplatePatternCreator headerTemplatePatternCreator,
        IDynamicRouting routing)
    {
        _cache = new();
        _routeKeyCreator = routeKeyCreator;
        _headerTemplatePatternCreator = headerTemplatePatternCreator;
        _routing = routing;
    }

    public Route Get(HttpRequest request, out IEnumerable<Error> errors)
    {
        var configuration = request.HttpContext.Items.IInternalConfiguration();
        var response = Find(configuration, request.Path, request.QueryString, new(request.Method), UpstreamHost(request), request.Headers);
        errors = response.Errors;
        return response.Data;
    }

    [Obsolete(Use_Get_HttpRequest_IEnumerable_method)]
    public Response<Route> Get(string upstreamUrlPath, string upstreamQueryString, string httpMethod,
        IInternalConfiguration configuration, string upstreamHost, IHeaderDictionary upstreamHeaders)
    {
        throw new NotSupportedException(Use_Get_HttpRequest_IEnumerable_method); // Aggressively ask developers to stop using it!
    }
    internal const string Use_Get_HttpRequest_IEnumerable_method = "Use the Get(HttpRequest, IEnumerable<Error>) method instead of this one! Note that this method has been deprecated since version 26.0!";

    protected virtual Response<Route> Find(IInternalConfiguration configuration,
        PathString upstreamUrlPath, QueryString upstreamQueryString, HttpMethod method, HostString upstreamHost, IHeaderDictionary upstreamHeaders)
    {
        var service = GetServiceName(upstreamUrlPath);
        var downstreamPath = new DownstreamPathTemplate(GetDownstreamPath, false); // Parse downstream path dynamically for each request, don't cache value

        var dynamicRoute = configuration.Routes
            .Where(r => r.IsDynamic) // process dynamic routes only
            .SelectMany(r => r.DownstreamRoute)
            .FirstOrDefault(dr => dr.ServiceName == service.Name && (service.Namespace.IsEmpty() || dr.ServiceNamespace == service.Namespace));

        var loadBalancerKey = dynamicRoute != null
            ? dynamicRoute.LoadBalancerKey
            : _routeKeyCreator.Create(service.Namespace, service.Name, configuration.LoadBalancerOptions);

        if (_cache.TryGetValue(loadBalancerKey, out var cachedRoute))
            return new OkResponse<Route>(cachedRoute);

        // TODO: Could it be that the static route functionality was possibly lost here? -> StaticRoutesCreator.SetUpRoute -> _upstreamTemplatePatternCreator
        var upstreamPathTemplate = new UpstreamPathTemplateBuilder().WithOriginalValue(upstreamUrlPath).Build();

        // TODO Under development. Implementation returns empty collection.
        var upstreamHeaderTemplates = _headerTemplatePatternCreator.Create(upstreamHeaders, false); // ? discoveryDownstreamRoute.UpstreamHeaders

        var routeBuilder = new DownstreamRouteBuilder()
            .WithServiceName(service.Name)
            .WithServiceNamespace(service.Namespace)
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
                .WithServiceName(service.Name/*dynamicRoute.ServiceName*/)
                .WithServiceNamespace(service.Namespace/*dynamicRoute.ServiceNamespace*/)
                .WithTimeout(dynamicRoute.Timeout);
        }

        var downstreamRoute = routeBuilder.Build();
        var route = new Route(true, downstreamRoute) // IsDynamic -> true
        {
            DownstreamPathTemplate = downstreamPath,
            UpstreamHeaderTemplates = upstreamHeaderTemplates,
            UpstreamHost = upstreamHost.Host,
            UpstreamHttpMethod = [method],
            UpstreamTemplatePattern = upstreamPathTemplate,
        };
        var cached = _cache.GetOrAdd(loadBalancerKey, route);
        return new OkResponse<Route>(cached);
    }

    protected virtual string GetDownstreamPath()
    {
        return _routing.GetDownstreamPath();
    }

    protected virtual ServiceInfo GetServiceName() // TODO: We must always call this to guarantee full support for all kinds of dynamic routing, including URL-path-based routing.
    {
        return _routing.GetServiceName();
    }

    /// <summary>Gets service name and its namespace of request URL.
    /// <para>Note: A namespace and service name should be separated by a '.' (dot) character.</para></summary>
    /// <remarks>Example: <c>http://ocelot.net/namespace.service-name/path</c> URL.</remarks>
    /// <param name="upstreamUrlPath">The upstream path.</param>
    /// <returns>A <see cref="ServiceInfo"/> value.</returns>
    protected virtual ServiceInfo GetServiceName(string upstreamUrlPath)
    {
        return _routing.GetServiceName(upstreamUrlPath); // TODO Ignore or not ignore? That's the question! See parameterless version
    }

    // protected string GetServiceName(HttpRequest request, out string serviceNamespace) => string.Empty;
    protected virtual HostString UpstreamHost(HttpRequest request) => request.Host;
}
