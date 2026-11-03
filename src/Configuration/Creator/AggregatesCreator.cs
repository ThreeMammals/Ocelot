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
    protected virtual HttpMethod DefaultHttpMethod { get; set; } = HttpMethod.Get;

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

    protected virtual bool TryCreateRoute(FileAggregateRoute aggregateRoute, FileGlobalConfiguration globalConfiguration,
        IReadOnlyDictionary<string, DownstreamRoute> routeByKey, out Route route)
    {
        route = null;
        var applicableRoutes = new List<DownstreamRoute>(aggregateRoute.RouteKeys.Count);
        foreach (var key in aggregateRoute.RouteKeys)
        {
            if (!routeByKey.TryGetValue(key, out var downstreamRoute))
                return false; // missing route -> skip aggregate

            applicableRoutes.Add(downstreamRoute);
        }

        var upstreamTemplatePattern = _creator.Create(aggregateRoute);
        var upstreamHeaderTemplates = _headerCreator.Create(aggregateRoute);
        var mergedMethods = CreateUpstreamHttpMethod(aggregateRoute, globalConfiguration);

        route = new Route
        {
            Aggregator = aggregateRoute.Aggregator,
            DownstreamRoute = applicableRoutes,
            DownstreamRouteConfig = aggregateRoute.RouteKeysConfig,
            UpstreamHeaderTemplates = upstreamHeaderTemplates,
            UpstreamHost = aggregateRoute.UpstreamHost,
            UpstreamHttpMethod = mergedMethods,
            UpstreamTemplatePattern = upstreamTemplatePattern,
        };
        return true;
    }

    protected virtual HashSet<HttpMethod> CreateUpstreamHttpMethod(FileAggregateRoute route, FileGlobalConfiguration global)
        => route.UpstreamHttpMethod.Count == 0 && global.UpstreamHttpMethod.Count == 0
            ? [DefaultHttpMethod] // both route and global configs are empty -> use default GET method
            : route.UpstreamHttpMethod.Union(global.UpstreamHttpMethod, StringComparer.OrdinalIgnoreCase).ToHttpMethods();
}
