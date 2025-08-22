using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;
using Ocelot.Infrastructure.Extensions;

namespace Ocelot.Configuration.Creator;

public class DynamicsCreator : IDynamicsCreator
{
    private readonly IRateLimitOptionsCreator _rateLimitOptionsCreator;
    private readonly IVersionCreator _versionCreator;
    private readonly IVersionPolicyCreator _versionPolicyCreator;
    private readonly IMetadataCreator _metadataCreator;

    public DynamicsCreator(
        IRateLimitOptionsCreator rateLimitOptionsCreator,
        IVersionCreator versionCreator,
        IVersionPolicyCreator versionPolicyCreator,
        IMetadataCreator metadataCreator)
    {
        _rateLimitOptionsCreator = rateLimitOptionsCreator;
        _versionCreator = versionCreator;
        _versionPolicyCreator = versionPolicyCreator;
        _metadataCreator = metadataCreator;
    }

    public IReadOnlyList<Route> Create(FileConfiguration fileConfiguration)
    {
        return fileConfiguration.DynamicRoutes
            .Select(dynamic => SetUpDynamicRoute(dynamic, fileConfiguration.GlobalConfiguration))
            .ToList();
    }

    public virtual int CreateTimeout(FileDynamicRoute route, FileGlobalConfiguration global)
    {
        int def = DownstreamRoute.DefaultTimeoutSeconds;
        return route.Timeout.Positive(def) ?? global.Timeout.Positive(def) ?? def;
    }

    private Route SetUpDynamicRoute(FileDynamicRoute dynamicRoute, FileGlobalConfiguration globalConfiguration)
    {
        var rateLimitOption = _rateLimitOptionsCreator.Create(dynamicRoute, globalConfiguration);
        var version = _versionCreator.Create(dynamicRoute.DownstreamHttpVersion);
        var versionPolicy = _versionPolicyCreator.Create(dynamicRoute.DownstreamHttpVersionPolicy);
        var metadata = _metadataCreator.Create(dynamicRoute.Metadata, globalConfiguration);

        var downstreamRoute = new DownstreamRouteBuilder()
            .WithRateLimitOptions(rateLimitOption)
            .WithServiceName(dynamicRoute.ServiceName)
            .WithDownstreamHttpVersion(version)
            .WithDownstreamHttpVersionPolicy(versionPolicy)
            .WithMetadata(metadata)
            .WithTimeout(CreateTimeout(dynamicRoute, globalConfiguration))
            .Build();

        return new Route(
            new() { downstreamRoute },
            new(),
            new List<HttpMethod>(),
            upstreamTemplatePattern: default,
            upstreamHost: default,
            aggregator: default,
            upstreamHeaderTemplates: default);
    }
}
