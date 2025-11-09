using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Responses;
using Ocelot.Values;

namespace Ocelot.DownstreamRouteFinder.Finder;

public class DiscoveryDownstreamRouteFinder : IDownstreamRouteProvider
{
    public const char Dot = '.';
    public const char Slash = '/';
    public const char Question = '?';

    private readonly ConcurrentDictionary<string, OkResponse<DownstreamRouteHolder>> _cache;
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

    public Response<DownstreamRouteHolder> Get(string upstreamUrlPath, string upstreamQueryString, string upstreamHttpMethod,
        IInternalConfiguration configuration, string upstreamHost, IDictionary<string, string> upstreamHeaders)
    {
        var serviceName = GetServiceName(upstreamUrlPath, out var serviceNamespace);
        var downstreamPath = GetDownstreamPath(upstreamUrlPath);
        var dynamicRoute = configuration.Routes
            .Where(r => r.IsDynamic) // process dynamic routes only
            .SelectMany(r => r.DownstreamRoute)
            .FirstOrDefault(dr => dr.ServiceName == serviceName && (serviceNamespace.IsEmpty() || dr.ServiceNamespace == serviceNamespace));
        var loadBalancerKey = dynamicRoute != null
            ? dynamicRoute.LoadBalancerKey
            : _routeKeyCreator.Create(serviceNamespace, serviceName, configuration.LoadBalancerOptions);
        if (_cache.TryGetValue(loadBalancerKey, out var downstreamRouteHolder))
        {
            return downstreamRouteHolder;
        }

        // TODO: Could it be that the static route functionality was possibly lost here? -> StaticRoutesCreator.SetUpRoute -> _upstreamTemplatePatternCreator
        var upstreamPathTemplate = new UpstreamPathTemplateBuilder().WithOriginalValue(upstreamUrlPath).Build();
        var upstreamHeaderTemplates = _upstreamHeaderTemplatePatternCreator.Create(upstreamHeaders, false); // ? discoveryDownstreamRoute.UpstreamHeaders

        var routeBuilder = new DownstreamRouteBuilder()
            .WithUseServiceDiscovery(true)
            .WithServiceName(serviceName)
            .WithServiceNamespace(serviceNamespace)
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
        downstreamRouteHolder = new OkResponse<DownstreamRouteHolder>(new DownstreamRouteHolder(new List<PlaceholderNameAndValue>(), route));
        _cache.AddOrUpdate(loadBalancerKey, downstreamRouteHolder, (x, y) => downstreamRouteHolder);
        return downstreamRouteHolder;
    }

    private static string GetDownstreamPath(string upstreamUrlPath)
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
        var path = upstreamUrlPath.AsSpan();
        int index = path[1..].IndexOf(Slash);
        var name = index == -1
            ? path[1..]
            : path.Slice(1, index).TrimEnd(Slash);

        index = name.IndexOf(Dot);
        serviceNamespace = index == -1
            ? string.Empty
            : name[..index].ToString();
        var serviceName = index == -1 ? name : name[++index..];
        return serviceName.ToString();
    }
}
