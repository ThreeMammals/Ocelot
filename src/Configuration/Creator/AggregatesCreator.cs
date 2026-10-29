using Ocelot.Configuration.File;
using Ocelot.Infrastructure.Extensions;

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

    /// <summary>This allows to override global default HTTP verb value.</summary>
    /// <remarks>Defaults: The <see cref="HttpMethod.Get"/> value.</remarks>
    /// <value>A <see cref="HttpMethod"/> value.</value>
    protected static HttpMethod DefaultHttpMethod { get; set; } = HttpMethod.Get;

    public IEnumerable<Route> Create(FileConfiguration configuration, IReadOnlyList<Route> routes)
    {
        // Build lookup once -> O(R) instead of repeated scans
        var routeByKey = routes
            .SelectMany(r => r.DownstreamRoute)
            .Where(dr => dr.Key.IsNotEmpty())
            .GroupBy(dr => dr.Key)
            .ToDictionary(g => g.Key, g => g.First()); // or throw on duplicates if you prefer strictness

        foreach (var aggregate in configuration.Aggregates)
        {
            if (TryCreateRoute(aggregate, configuration.GlobalConfiguration, routeByKey, out var route))
                yield return route;
        }
    }

    protected virtual bool TryCreateRoute(FileAggregateRoute aggregateRoute, FileGlobalConfiguration global,
        IReadOnlyDictionary<string, DownstreamRoute> routeByKey, out Route route)
    {
        route = null;

        // Avoid mutating the input configuration object
        ICollection<string> httpMethods = aggregateRoute.UpstreamHttpMethod.Count == 0
            ? [DefaultHttpMethod.ToString()]
            : aggregateRoute.UpstreamHttpMethod;

        var applicableRoutes = new List<DownstreamRoute>(aggregateRoute.RouteKeys.Count);
        foreach (var key in aggregateRoute.RouteKeys)
        {
            if (!routeByKey.TryGetValue(key, out var downstreamRoute))
                return false; // missing route -> skip aggregate

            applicableRoutes.Add(downstreamRoute);
        }

        // Call injected services first so you can extend/modify the results before route creation
        var upstreamTemplatePattern = _creator.Create(aggregateRoute);
        var upstreamHeaderTemplates = _headerCreator.Create(aggregateRoute);
        var upstreamHttpMethod = httpMethods.ToHttpMethods();

        // TODO: extend upstreamTemplatePattern / upstreamHeaderTemplates here as needed

        route = new Route
        {
            Aggregator = aggregateRoute.Aggregator,
            DownstreamRoute = applicableRoutes,
            DownstreamRouteConfig = aggregateRoute.RouteKeysConfig,
            UpstreamHeaderTemplates = upstreamHeaderTemplates,
            UpstreamHost = aggregateRoute.UpstreamHost,
            UpstreamHttpMethod = upstreamHttpMethod,
            UpstreamTemplatePattern = upstreamTemplatePattern,
        };
        return true;
    }
}
