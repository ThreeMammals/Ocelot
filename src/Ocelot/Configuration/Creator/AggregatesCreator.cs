using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public class AggregatesCreator : IAggregatesCreator
{
    private readonly IUpstreamTemplatePatternCreator _creator;
    private readonly IUpstreamHeaderTemplatePatternCreator _headerCreator;

    public AggregatesCreator(IUpstreamTemplatePatternCreator creator, IUpstreamHeaderTemplatePatternCreator headerCreator)
    {
        _creator = creator;
        _headerCreator = headerCreator;
    }

    public List<Route> Create(FileConfiguration fileConfiguration, IReadOnlyList<Route> routes)
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
        var upstreamHeaderTemplates = _headerCreator.Create(aggregateRoute);
        var upstreamHttpMethod = (aggregateRoute.UpstreamHttpMethod.Count == 0) ? new List<HttpMethod>()
            : aggregateRoute.UpstreamHttpMethod.Select(x => new HttpMethod(x.Trim())).ToList();

        return new Route(
            applicableRoutes,
            aggregateRoute.RouteKeysConfig,
            upstreamHttpMethod,
            upstreamTemplatePattern,
            aggregateRoute.UpstreamHost,
            aggregateRoute.Aggregator,
            upstreamHeaderTemplates);
    }
}
