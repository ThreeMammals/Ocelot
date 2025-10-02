using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Infrastructure.Extensions;
using Ocelot.LoadBalancer.Balancers;
using Ocelot.Responses;
using Ocelot.Values;

namespace Ocelot.DownstreamRouteFinder.Finder;

public class DiscoveryDownstreamRouteFinder : IDownstreamRouteProvider
{
    public const char Dot = '.';
    public const char Slash = '/';
    public const char Question = '?';

    private readonly ConcurrentDictionary<string, OkResponse<DownstreamRouteHolder>> _cache;
    private readonly IUpstreamHeaderTemplatePatternCreator _upstreamHeaderTemplatePatternCreator;

    public DiscoveryDownstreamRouteFinder(IUpstreamHeaderTemplatePatternCreator upstreamHeaderTemplatePatternCreator)
    {
        _cache = new();
        _upstreamHeaderTemplatePatternCreator = upstreamHeaderTemplatePatternCreator;
    }

    public Response<DownstreamRouteHolder> Get(string upstreamUrlPath, string upstreamQueryString, string upstreamHttpMethod,
        IInternalConfiguration configuration, string upstreamHost, IDictionary<string, string> upstreamHeaders)
    {
        var serviceName = GetServiceName(upstreamUrlPath, out var serviceNamespace);
        var downstreamPath = GetDownstreamPath(upstreamUrlPath);
        if (HasQueryString(downstreamPath))
        {
            downstreamPath = RemoveQueryString(downstreamPath);
        }

        var downstreamPathForKeys = $"/{serviceNamespace}{Dot}{serviceName}{downstreamPath}";
        var loadBalancerKey = CreateLoadBalancerKey(downstreamPathForKeys, upstreamHttpMethod, configuration.LoadBalancerOptions);
        if (_cache.TryGetValue(loadBalancerKey, out var downstreamRouteHolder))
        {
            return downstreamRouteHolder;
        }

        var qosOptions = new QoSOptions(configuration.QoSOptions)
        {
            Key = $"{downstreamPathForKeys}|{upstreamHttpMethod}",
        };

        // TODO: Could it be that the static route functionality was possibly lost here? -> StaticRoutesCreator.SetUpRoute -> _upstreamTemplatePatternCreator
        var upstreamPathTemplate = new UpstreamPathTemplateBuilder().WithOriginalValue(upstreamUrlPath).Build();
        var upstreamHeaderTemplates = _upstreamHeaderTemplatePatternCreator.Create(upstreamHeaders, false); // ? discoveryDownstreamRoute.UpstreamHeaders

        var routeBuilder = new DownstreamRouteBuilder()
            .WithServiceName(serviceName)
            .WithServiceNamespace(serviceNamespace)
            .WithLoadBalancerKey(loadBalancerKey)
            .WithDownstreamPathTemplate(downstreamPath)
            .WithUseServiceDiscovery(true)
            .WithHttpHandlerOptions(configuration.HttpHandlerOptions)
            .WithQosOptions(qosOptions)
            .WithDownstreamScheme(configuration.DownstreamScheme)
            .WithLoadBalancerOptions(configuration.LoadBalancerOptions)
            .WithDownstreamHttpVersion(configuration.DownstreamHttpVersion)
            .WithUpstreamPathTemplate(upstreamPathTemplate)
            .WithUpstreamHeaders(upstreamHeaderTemplates as Dictionary<string, UpstreamHeaderTemplate>);

        // TODO: Review this logic. Is this merging options for dynamic routes?
        var dynamicRoute = configuration.Routes?
            .SelectMany(x => x.DownstreamRoute)
            .FirstOrDefault(x => x.ServiceName == serviceName && (serviceNamespace.IsEmpty() || x.ServiceNamespace == serviceNamespace));
        if (dynamicRoute != null)
        {
            // We are set to replace IInternalConfiguration global options with the current options from actual dynamic route
            routeBuilder
                .WithRateLimitOptions(dynamicRoute.RateLimitOptions)
                .WithLoadBalancerOptions(dynamicRoute.LoadBalancerOptions);
        }

        var downstreamRoute = routeBuilder.Build();
        var route = new Route(downstreamRoute)
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

    private static string RemoveQueryString(string downstreamPath)
    {
        int index = downstreamPath.IndexOf(Question);
        return downstreamPath[..index];
    }

    private static bool HasQueryString(string downstreamPath) => downstreamPath.Contains(Question);

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

    private static string CreateLoadBalancerKey(string downstreamTemplatePath, string httpMethod, LoadBalancerOptions options)
    {
        if (!string.IsNullOrEmpty(options.Type) && !string.IsNullOrEmpty(options.Key) && options.Type == nameof(CookieStickySessions))
        {
            return $"{nameof(CookieStickySessions)}:{options.Key}";
        }

        return CreateQoSKey(downstreamTemplatePath, httpMethod);
    }

    private static string CreateQoSKey(string downstreamTemplatePath, string httpMethod)
    {
        var loadBalancerKey = $"{downstreamTemplatePath}|{httpMethod}";
        return loadBalancerKey;
    }
}
