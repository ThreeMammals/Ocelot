using System.Collections.Generic;
using System.Linq;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{

    public class DynamicsCreator : IDynamicsCreator
    {
        private readonly IRateLimitOptionsCreator _rateLimitOptionsCreator;

        public DynamicsCreator(IRateLimitOptionsCreator rateLimitOptionsCreator)
        {
            _rateLimitOptionsCreator = rateLimitOptionsCreator;
        }

        public List<ReRoute> Dynamics(FileConfiguration fileConfiguration)
        {
            return fileConfiguration.DynamicReRoutes
                .Select(dynamic => SetUpDynamicReRoute(dynamic, fileConfiguration.GlobalConfiguration)).ToList();
        }

        private ReRoute SetUpDynamicReRoute(FileDynamicReRoute fileDynamicReRoute, FileGlobalConfiguration globalConfiguration)
        {
            var rateLimitOption = _rateLimitOptionsCreator.Create(fileDynamicReRoute.RateLimitRule, globalConfiguration);

            var downstreamReRoute = new DownstreamReRouteBuilder()
                .WithEnableRateLimiting(true)
                .WithRateLimitOptions(rateLimitOption)
                .WithServiceName(fileDynamicReRoute.ServiceName)
                .Build();

            var reRoute = new ReRouteBuilder()
                .WithDownstreamReRoute(downstreamReRoute)
                .Build();

            return reRoute;
        }
    }
}
