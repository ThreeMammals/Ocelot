using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
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

        public List<Route> Create(FileConfiguration fileConfiguration)
        {
            return fileConfiguration.DynamicRoutes
                .Select(dynamic => SetUpDynamicRoute(dynamic, fileConfiguration.GlobalConfiguration))
                .ToList();
        }

        public int CreateTimeout(FileDynamicRoute route, FileGlobalConfiguration global)
            => route.Timeout ?? global.Timeout ?? DownstreamRoute.DefaultTimeoutSeconds;

        private Route SetUpDynamicRoute(FileDynamicRoute dynamicRoute, FileGlobalConfiguration globalConfiguration)
        {
            var rateLimitOption = _rateLimitOptionsCreator
                .Create(dynamicRoute.RateLimitRule, globalConfiguration);

            var version = _versionCreator.Create(dynamicRoute.DownstreamHttpVersion);
            var versionPolicy = _versionPolicyCreator.Create(dynamicRoute.DownstreamHttpVersionPolicy);
            var metadata = _metadataCreator.Create(dynamicRoute.Metadata, globalConfiguration);

            var downstreamRoute = new DownstreamRouteBuilder()
                .WithEnableRateLimiting(rateLimitOption.EnableRateLimiting)
                .WithRateLimitOptions(rateLimitOption)
                .WithServiceName(dynamicRoute.ServiceName)
                .WithDownstreamHttpVersion(version)
                .WithDownstreamHttpVersionPolicy(versionPolicy)
                .WithMetadata(metadata)
                .WithTimeout(CreateTimeout(dynamicRoute, globalConfiguration))
                .Build();

            var route = new RouteBuilder()
                .WithDownstreamRoute(downstreamRoute)
                .Build();

            return route;
        }
    }
}
