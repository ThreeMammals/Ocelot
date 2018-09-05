using System.Collections.Generic;
using System.Linq;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{

    public class AggregatesCreator : IAggregatesCreator
    {
        private readonly IUpstreamTemplatePatternCreator _creator;

        public AggregatesCreator(IUpstreamTemplatePatternCreator creator)
        {
            _creator = creator;
        }

        public List<ReRoute> Aggregates(FileConfiguration fileConfiguration, List<ReRoute> reRoutes)
        {
            return fileConfiguration.Aggregates
                .Select(aggregate => SetUpAggregateReRoute(reRoutes, aggregate, fileConfiguration.GlobalConfiguration))
                .ToList();
        }

        private ReRoute SetUpAggregateReRoute(IEnumerable<ReRoute> reRoutes, FileAggregateReRoute aggregateReRoute, FileGlobalConfiguration globalConfiguration)
        {
            var applicableReRoutes = reRoutes
                .SelectMany(x => x.DownstreamReRoute)
                .Where(r => aggregateReRoute.ReRouteKeys.Contains(r.Key))
                .ToList();

            if (applicableReRoutes.Count != aggregateReRoute.ReRouteKeys.Count)
            {
                //todo - log or throw or return error whatever?
            }

            //make another re route out of these
            var upstreamTemplatePattern = _creator.Create(aggregateReRoute);

            var reRoute = new ReRouteBuilder()
                .WithUpstreamHttpMethod(aggregateReRoute.UpstreamHttpMethod)
                .WithUpstreamPathTemplate(upstreamTemplatePattern)
                .WithDownstreamReRoutes(applicableReRoutes)
                .WithUpstreamHost(aggregateReRoute.UpstreamHost)
                .WithAggregator(aggregateReRoute.Aggregator)
                .Build();

            return reRoute;
        }
    }
}
