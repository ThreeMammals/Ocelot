using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public class DynamicsCreator : IDynamicsCreator
    {
        private readonly IRateLimitOptionsCreator _rateLimitOptionsCreator;
        private readonly IVersionCreator _versionCreator;
        private readonly IVersionPolicyCreator _versionPolicyCreator;

        public DynamicsCreator(IRateLimitOptionsCreator rateLimitOptionsCreator, IVersionCreator versionCreator, IVersionPolicyCreator versionPolicyCreator)
        {
            _rateLimitOptionsCreator = rateLimitOptionsCreator;
            _versionCreator = versionCreator;
            _versionPolicyCreator = versionPolicyCreator;
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
            var versionPolicy = _versionPolicyCreator.Create(fileDynamicRoute.DownstreamVersionPolicy);

            var downstreamRoute = new DownstreamRouteBuilder()
                .WithEnableRateLimiting(rateLimitOption.EnableRateLimiting)
                .WithRateLimitOptions(rateLimitOption)
                .WithServiceName(fileDynamicRoute.ServiceName)
                .WithDownstreamHttpVersion(version)
                .WithDownstreamPolicyVersion(versionPolicy)
                .Build();

            var route = new RouteBuilder()
                .WithDownstreamRoute(downstreamRoute)
                .Build();

            return route;
        }
    }
}
