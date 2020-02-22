namespace Ocelot.Configuration.Creator
{
    using Builder;
    using File;
    using System.Collections.Generic;
    using System.Linq;

    public class DynamicsCreator : IDynamicsCreator
    {
        private readonly IRateLimitOptionsCreator _rateLimitOptionsCreator;
        private readonly IVersionCreator _versionCreator;

        public DynamicsCreator(IRateLimitOptionsCreator rateLimitOptionsCreator, IVersionCreator versionCreator)
        {
            _rateLimitOptionsCreator = rateLimitOptionsCreator;
            _versionCreator = versionCreator;
        }

        public List<ReRoute> Create(FileConfiguration fileConfiguration)
        {
            return fileConfiguration.DynamicReRoutes
                .Select(dynamic => SetUpDynamicReRoute(dynamic, fileConfiguration.GlobalConfiguration))
                .ToList();
        }

        private ReRoute SetUpDynamicReRoute(FileDynamicReRoute fileDynamicReRoute, FileGlobalConfiguration globalConfiguration)
        {
            var rateLimitOption = _rateLimitOptionsCreator
                .Create(fileDynamicReRoute.RateLimitRule, globalConfiguration);

            var version = _versionCreator.Create(fileDynamicReRoute.DownstreamHttpVersion);

            var downstreamReRoute = new DownstreamReRouteBuilder()
                .WithEnableRateLimiting(rateLimitOption.EnableRateLimiting)
                .WithRateLimitOptions(rateLimitOption)
                .WithServiceName(fileDynamicReRoute.ServiceName)
                .WithDownstreamHttpVersion(version)
                .Build();

            var reRoute = new ReRouteBuilder()
                .WithDownstreamReRoute(downstreamReRoute)
                .Build();

            return reRoute;
        }
    }
}
