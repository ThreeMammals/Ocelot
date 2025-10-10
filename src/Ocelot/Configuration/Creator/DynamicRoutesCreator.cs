using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;
using Ocelot.Infrastructure.Extensions;

namespace Ocelot.Configuration.Creator;

public class DynamicRoutesCreator : IDynamicsCreator
{
    private readonly IRouteKeyCreator _routeKeyCreator;
    private readonly ILoadBalancerOptionsCreator _loadBalancerOptionsCreator;
    private readonly IRateLimitOptionsCreator _rateLimitOptionsCreator;
    private readonly IVersionCreator _versionCreator;
    private readonly IVersionPolicyCreator _versionPolicyCreator;
    private readonly IMetadataCreator _metadataCreator;

    public DynamicRoutesCreator(
        IRouteKeyCreator routeKeyCreator,
        ILoadBalancerOptionsCreator loadBalancerOptionsCreator,
        IRateLimitOptionsCreator rateLimitOptionsCreator,
        IVersionCreator versionCreator,
        IVersionPolicyCreator versionPolicyCreator,
        IMetadataCreator metadataCreator)
    {
        _routeKeyCreator = routeKeyCreator;
        _loadBalancerOptionsCreator = loadBalancerOptionsCreator;
        _rateLimitOptionsCreator = rateLimitOptionsCreator;
        _versionCreator = versionCreator;
        _versionPolicyCreator = versionPolicyCreator;
        _metadataCreator = metadataCreator;
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

        var lbOptions = _loadBalancerOptionsCreator.Create(dynamicRoute, globalConfiguration);
        var lbKey = _routeKeyCreator.Create(dynamicRoute, lbOptions);
        var rateLimitOptions = _rateLimitOptionsCreator.Create(dynamicRoute, globalConfiguration);
        var version = _versionCreator.Create(dynamicRoute.DownstreamHttpVersion);
        var versionPolicy = _versionPolicyCreator.Create(dynamicRoute.DownstreamHttpVersionPolicy);
        var metadata = _metadataCreator.Create(dynamicRoute.Metadata, globalConfiguration);

        var downstreamRoute = new DownstreamRouteBuilder()
            .WithServiceName(dynamicRoute.ServiceName)
            .WithServiceNamespace(dynamicRoute.ServiceNamespace)
            .WithLoadBalancerKey(lbKey)
            .WithLoadBalancerOptions(lbOptions)
            .WithRateLimitOptions(rateLimitOptions)
            .WithDownstreamHttpVersion(version)
            .WithDownstreamHttpVersionPolicy(versionPolicy)
            .WithMetadata(metadata)
            .WithTimeout(CreateTimeout(dynamicRoute, globalConfiguration))
            .Build();
        return new Route(true, downstreamRoute); // IsDynamic -> true
    }
}
