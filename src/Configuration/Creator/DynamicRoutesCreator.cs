using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;
using Ocelot.Infrastructure.Extensions;

namespace Ocelot.Configuration.Creator;

public class DynamicRoutesCreator : IDynamicsCreator
{
    private readonly IAuthenticationOptionsCreator _authOptionsCreator;
    private readonly ICacheOptionsCreator _cacheOptionsCreator;
    private readonly IHttpHandlerOptionsCreator _httpHandlerOptionsCreator;
    private readonly ILoadBalancerOptionsCreator _loadBalancerOptionsCreator;
    private readonly IMetadataCreator _metadataCreator;
    private readonly IQoSOptionsCreator _qosOptionsCreator;
    private readonly IRateLimitOptionsCreator _rateLimitOptionsCreator;
    private readonly IRouteKeyCreator _loadBalancerKeyCreator;
    private readonly IVersionCreator _versionCreator;
    private readonly IVersionPolicyCreator _versionPolicyCreator;

    public DynamicRoutesCreator(
        IAuthenticationOptionsCreator authOptionsCreator,
        ICacheOptionsCreator cacheOptionsCreator,
        IHttpHandlerOptionsCreator handlerOptionsCreator,
        ILoadBalancerOptionsCreator loadBalancerOptionsCreator,
        IMetadataCreator metadataCreator,
        IQoSOptionsCreator qosOptionsCreator,
        IRateLimitOptionsCreator rateLimitOptionsCreator,
        IRouteKeyCreator loadBalancerKeyCreator,
        IVersionCreator versionCreator,
        IVersionPolicyCreator versionPolicyCreator)
    {
        _authOptionsCreator = authOptionsCreator;
        _cacheOptionsCreator = cacheOptionsCreator;
        _httpHandlerOptionsCreator = handlerOptionsCreator;
        _loadBalancerKeyCreator = loadBalancerKeyCreator;
        _loadBalancerOptionsCreator = loadBalancerOptionsCreator;
        _metadataCreator = metadataCreator;
        _qosOptionsCreator = qosOptionsCreator;
        _rateLimitOptionsCreator = rateLimitOptionsCreator;
        _versionCreator = versionCreator;
        _versionPolicyCreator = versionPolicyCreator;
    }

    public IReadOnlyList<Route> Create(FileConfiguration fileConfiguration)
    {
        Route CreateRoute(FileDynamicRoute route)
            => SetUpDynamicRoute(route, fileConfiguration.GlobalConfiguration);
        return fileConfiguration.DynamicRoutes
            .Select(CreateRoute)
            .ToArray();
    }

    public virtual int CreateTimeout(FileDynamicRoute route, FileGlobalConfiguration global)
    {
        int def = DownstreamRoute.DefaultTimeoutSeconds;
        return route.Timeout.Positive(def) ?? global.Timeout.Positive(def) ?? def;
    }

    private Route SetUpDynamicRoute(FileDynamicRoute dynamicRoute, FileGlobalConfiguration globalConfiguration)
    {
        // The old RateLimitRule property takes precedence over the new RateLimitOptions property for backward compatibility, thus, override forcibly
        if (dynamicRoute.RateLimitRule != null)
        {
            dynamicRoute.RateLimitOptions = dynamicRoute.RateLimitRule;
        }

        // Load balancing dependants
        var lbOptions = _loadBalancerOptionsCreator.Create(dynamicRoute, globalConfiguration);
        var lbKey = _loadBalancerKeyCreator.Create(dynamicRoute, lbOptions);
        var cacheOptions = _cacheOptionsCreator.Create(dynamicRoute, globalConfiguration, lbKey);

        var authOptions = _authOptionsCreator.Create(dynamicRoute, globalConfiguration);
        var version = _versionCreator.Create(dynamicRoute.DownstreamHttpVersion.IfEmpty(globalConfiguration.DownstreamHttpVersion));
        var versionPolicy = _versionPolicyCreator.Create(dynamicRoute.DownstreamHttpVersionPolicy.IfEmpty(globalConfiguration.DownstreamHttpVersionPolicy));
        var scheme = dynamicRoute.DownstreamScheme.IfEmpty(globalConfiguration.DownstreamScheme);
        var handlerOptions = _httpHandlerOptionsCreator.Create(dynamicRoute, globalConfiguration);
        var metadata = _metadataCreator.Create(dynamicRoute.Metadata, globalConfiguration);
        var qosOptions = _qosOptionsCreator.Create(dynamicRoute, globalConfiguration);
        var rlOptions = _rateLimitOptionsCreator.Create(dynamicRoute, globalConfiguration);
        var timeout = CreateTimeout(dynamicRoute, globalConfiguration);
        var downstreamRoute = new DownstreamRouteBuilder()
            .WithAuthenticationOptions(authOptions)
            .WithCacheOptions(cacheOptions)
            .WithDownstreamHttpVersion(version)
            .WithDownstreamHttpVersionPolicy(versionPolicy)
            .WithDownstreamScheme(scheme)
            .WithHttpHandlerOptions(handlerOptions)
            .WithLoadBalancerKey(lbKey)
            .WithLoadBalancerOptions(lbOptions)
            .WithMetadata(metadata)
            .WithQosOptions(qosOptions)
            .WithRateLimitOptions(rlOptions)
            .WithServiceName(dynamicRoute.ServiceName)
            .WithServiceNamespace(dynamicRoute.ServiceNamespace)
            .WithTimeout(timeout)
            .Build();
        return new Route(true, downstreamRoute); // IsDynamic -> true
    }
}
