namespace Ocelot.Configuration.Creator
{
    using Builder;
    using File;
    using System.Collections.Generic;
    using System.Linq;

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
            var applicableReRoutes = new List<DownstreamReRoute>();
            var allReRoutes = reRoutes.SelectMany(x => x.DownstreamReRoute);

            foreach (var reRouteKey in aggregateReRoute.ReRouteKeys)
            {
                var selec = allReRoutes.FirstOrDefault(q => q.Key == reRouteKey);
                if (selec == null)
                {
                    return null;
                }

                applicableReRoutes.Add(selec);
            }

            var upstreamTemplatePattern = _creator.Create(aggregateReRoute);

            var reRoute = new ReRouteBuilder()
                .WithUpstreamHttpMethod(aggregateReRoute.UpstreamHttpMethod)
                .WithUpstreamPathTemplate(upstreamTemplatePattern)
                .WithDownstreamReRoutes(applicableReRoutes)
                .WithAggregateReRouteConfig(aggregateReRoute.ReRouteKeysConfig)
                .WithUpstreamHost(aggregateReRoute.UpstreamHost)
                .WithAggregator(aggregateReRoute.Aggregator)
                .Build();

            return reRoute;
        }
    }
}
