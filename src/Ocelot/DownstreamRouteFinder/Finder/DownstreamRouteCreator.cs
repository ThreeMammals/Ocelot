using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Responses;
using Ocelot.Values;

namespace Ocelot.DownstreamRouteFinder.Finder;

/// <summary>
/// TODO: Rename to ServiceDiscoveryDownstreamRouteCreator or ServiceDiscoveryRoutesCreator or MultiplexingRoutesCreator (see DownstreamRouteHolder refs).
/// </summary>
public class DownstreamRouteCreator : IDownstreamRouteProvider
{
    private readonly ConcurrentDictionary<string, OkResponse<DownstreamRouteHolder>> _cache;
    private readonly IUpstreamHeaderTemplatePatternCreator _upstreamHeaderTemplatePatternCreator;

    public DownstreamRouteCreator(IUpstreamHeaderTemplatePatternCreator upstreamHeaderTemplatePatternCreator)
    {
        _cache = new();
        _upstreamHeaderTemplatePatternCreator = upstreamHeaderTemplatePatternCreator;
    }

    public Response<DownstreamRouteHolder> Get(string upstreamUrlPath, string upstreamQueryString, string upstreamHttpMethod,
        IInternalConfiguration configuration, string upstreamHost, IDictionary<string, string> upstreamHeaders)
    {
        var serviceName = GetServiceName(upstreamUrlPath);
        var downstreamPath = GetDownstreamPath(upstreamUrlPath);
        if (HasQueryString(downstreamPath))
        {
            downstreamPath = RemoveQueryString(downstreamPath);
        }

        var downstreamPathForKeys = $"/{serviceName}{downstreamPath}";
        var loadBalancerKey = CreateLoadBalancerKey(downstreamPathForKeys, upstreamHttpMethod, configuration.LoadBalancerOptions);
        if (_cache.TryGetValue(loadBalancerKey, out var downstreamRouteHolder))
        {
            return downstreamRouteHolder;
        }

        var qosOptions = new QoSOptions(configuration.QoSOptions)
        {
            Key = $"{downstreamPathForKeys}|{upstreamHttpMethod}",
        };

        // TODO: Could it be that the static route functionality was possibly lost here? -> RoutesCreator.SetUpRoute -> _upstreamTemplatePatternCreator
        var upstreamPathTemplate = new UpstreamPathTemplateBuilder().WithOriginalValue(upstreamUrlPath).Build();
        var upstreamHeaderTemplates = _upstreamHeaderTemplatePatternCreator.Create(upstreamHeaders, false); // ? serviceDiscoveryDownstreamRoute.UpstreamHeaders

        var downstreamRouteBuilder = new DownstreamRouteBuilder()
            .WithServiceName(serviceName)
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

        var serviceDiscoveryDownstreamRoute = configuration.Routes?
            .SelectMany(x => x.DownstreamRoute)
            .FirstOrDefault(x => x.ServiceName == serviceName);
        if (serviceDiscoveryDownstreamRoute != null)
        {
            downstreamRouteBuilder
                .WithRateLimitOptions(serviceDiscoveryDownstreamRoute.RateLimitOptions);
        }

        var downstreamRoute = downstreamRouteBuilder.Build();
        var route = new Route(
            new() { downstreamRoute },
            new(),
            new List<HttpMethod>() { new(upstreamHttpMethod.Trim()) },
            upstreamPathTemplate,
            upstreamHost,
            aggregator: default,
            upstreamHeaderTemplates);

        downstreamRouteHolder = new OkResponse<DownstreamRouteHolder>(new DownstreamRouteHolder(new List<PlaceholderNameAndValue>(), route));
        _cache.AddOrUpdate(loadBalancerKey, downstreamRouteHolder, (x, y) => downstreamRouteHolder);

        return downstreamRouteHolder;
    }

    private static string RemoveQueryString(string downstreamPath)
    {
        return downstreamPath
            .Substring(0, downstreamPath.IndexOf('?'));
    }

    private static bool HasQueryString(string downstreamPath)
    {
        return downstreamPath.Contains('?');
    }

    private static string GetDownstreamPath(string upstreamUrlPath)
    {
        if (upstreamUrlPath.IndexOf('/', 1) == -1)
        {
            return "/";
        }

        return upstreamUrlPath
            .Substring(upstreamUrlPath.IndexOf('/', 1));
    }

    private static string GetServiceName(string upstreamUrlPath)
    {
        if (upstreamUrlPath.IndexOf('/', 1) == -1)
        {
            return upstreamUrlPath
                .Substring(1);
        }

        return upstreamUrlPath
            .Substring(1, upstreamUrlPath.IndexOf('/', 1))
            .TrimEnd('/');
    }

    private static string CreateLoadBalancerKey(string downstreamTemplatePath, string httpMethod, LoadBalancerOptions loadBalancerOptions)
    {
        if (!string.IsNullOrEmpty(loadBalancerOptions.Type) && !string.IsNullOrEmpty(loadBalancerOptions.Key) && loadBalancerOptions.Type == nameof(CookieStickySessions))
        {
            return $"{nameof(CookieStickySessions)}:{loadBalancerOptions.Key}";
        }

        return CreateQoSKey(downstreamTemplatePath, httpMethod);
    }

    private static string CreateQoSKey(string downstreamTemplatePath, string httpMethod)
    {
        var loadBalancerKey = $"{downstreamTemplatePath}|{httpMethod}";
        return loadBalancerKey;
    }
}
