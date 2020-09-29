namespace Ocelot.Configuration.Creator
{
    using Ocelot.Configuration.Builder;
    using Ocelot.Configuration.File;
    using System.Collections.Generic;
    using System.Linq;

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

            //TODO: What is this logic?
            foreach (var routeId in aggregateRoute.RouteIds)
            {
                var applicableRoute = allRoutes.FirstOrDefault(q => q.RouteId.Value == routeId);
                if (applicableRoute == null)
                {
                    return null;
                }

                applicableRoutes.Add(applicableRoute);
            }

            var upstreamTemplatePattern = _creator.Create(aggregateRoute);

            //TODO: extract and test
            var aggregateRouteConfigs = aggregateRoute?.AggregateRouteConfigs?.Select(a => new AggregateRouteConfig(new RouteId(a.RouteId), a.Parameter, a.JsonPath));

            var route = new RouteBuilder()
                .WithUpstreamHttpMethod(aggregateRoute.UpstreamHttpMethod)
                .WithUpstreamPathTemplate(upstreamTemplatePattern)
                .WithDownstreamRoutes(applicableRoutes)
                .WithAggregateRouteConfig(aggregateRouteConfigs)
                .WithUpstreamHost(aggregateRoute.UpstreamHost)
                .WithAggregator(aggregateRoute.Aggregator)
                .Build();

            return route;
        }
    }
}
