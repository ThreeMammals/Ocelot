namespace Ocelot.Configuration.Creator
{
    using System.Collections.Generic;
    using System.Linq;
    using Builder;
    using File;

    public class AggregatesCreator : IAggregatesCreator
    {
        private readonly IUpstreamTemplatePatternCreator _creator;

        public AggregatesCreator(IUpstreamTemplatePatternCreator creator)
        {
            _creator = creator;
        }

        public List<ReRoute> Create(FileConfiguration fileConfiguration, List<ReRoute> reRoutes)
        {
            return fileConfiguration.Aggregates
                .Select(aggregate => SetUpAggregateReRoute(reRoutes, aggregate, fileConfiguration.GlobalConfiguration))
                .Where(aggregate => aggregate != null)
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
                return null;
            }

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
