using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public class DynamicsCreator : IDynamicsCreator
    {
        private readonly IRateLimitOptionsCreator _rateLimitOptionsCreator;
        private readonly IVersionCreator _versionCreator;
        private readonly IMetadataCreator _metadataCreator;

        public DynamicsCreator(
            IRateLimitOptionsCreator rateLimitOptionsCreator,
            IVersionCreator versionCreator,
            IMetadataCreator metadataCreator)
        {
            _rateLimitOptionsCreator = rateLimitOptionsCreator;
            _versionCreator = versionCreator;
            _metadataCreator = metadataCreator;
        }

        public List<Route> Create(FileConfiguration fileConfiguration)
        {
            return fileConfiguration.DynamicRoutes
                .Select(dynamic => SetUpDynamicRoute(dynamic, fileConfiguration.GlobalConfiguration))
                .ToList();
        }

        private Route SetUpDynamicRoute(FileDynamicRoute fileDynamicRoute, FileGlobalConfiguration globalConfiguration)
        {
            var rateLimitOption = _rateLimitOptionsCreator
                .Create(fileDynamicRoute.RateLimitRule, globalConfiguration);

            var version = _versionCreator.Create(fileDynamicRoute.DownstreamHttpVersion);

            var metadata = _metadataCreator.Create(fileDynamicRoute.Metadata, globalConfiguration);

            var downstreamRoute = new DownstreamRouteBuilder()
                .WithEnableRateLimiting(rateLimitOption.EnableRateLimiting)
                .WithRateLimitOptions(rateLimitOption)
                .WithServiceName(fileDynamicRoute.ServiceName)
                .WithDownstreamHttpVersion(version)
                .WithMetadata(metadata)
                .Build();

            var route = new RouteBuilder()
                .WithDownstreamRoute(downstreamRoute)
                .Build();

            return route;
        }
    }
}
