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

        public List<Route> Create(FileConfiguration fileConfiguration, List<Route> routes)
        {
            return fileConfiguration.Aggregates
                .Select(aggregate => SetUpAggregateRoute(routes, aggregate, fileConfiguration.GlobalConfiguration))
                .Where(aggregate => aggregate != null)
                .ToList();
        }

        private Route SetUpAggregateRoute(IEnumerable<Route> routes, FileAggregateRoute aggregateRoute, FileGlobalConfiguration globalConfiguration)
        {
            var applicableRoutes = new List<DownstreamRoute>();
            var allRoutes = routes.SelectMany(x => x.DownstreamRoute);
            var downstreamRoutes = aggregateRoute.RouteKeys.Select(routeKey => allRoutes.FirstOrDefault(q => q.Key == routeKey));
            foreach (var downstreamRoute in downstreamRoutes)
            {
                if (downstreamRoute == null)
                {
                    return null;
                }

                applicableRoutes.Add(downstreamRoute);
            }

            var upstreamTemplatePattern = _creator.Create(aggregateRoute);

            var route = new RouteBuilder()
                .WithUpstreamHttpMethod(aggregateRoute.UpstreamHttpMethod)
                .WithUpstreamPathTemplate(upstreamTemplatePattern)
                .WithDownstreamRoutes(applicableRoutes)
                .WithAggregateRouteConfig(aggregateRoute.RouteKeysConfig)
                .WithUpstreamHost(aggregateRoute.UpstreamHost)
                .WithAggregator(aggregateRoute.Aggregator)
                .Build();

            return route;
        }
    }
}
