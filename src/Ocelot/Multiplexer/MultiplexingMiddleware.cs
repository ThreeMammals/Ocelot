using System.Collections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Logging;
using Ocelot.Middleware;
using Route = Ocelot.Configuration.Route;

namespace Ocelot.Multiplexer;

public class MultiplexingMiddleware : OcelotMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IResponseAggregatorFactory _factory;
    private const string RequestIdString = "RequestId";

    public MultiplexingMiddleware(RequestDelegate next,
        IOcelotLoggerFactory loggerFactory,
        IResponseAggregatorFactory factory
    )
        : base(loggerFactory.CreateLogger<MultiplexingMiddleware>())
    {
        _factory = factory;
        _next = next;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        var downstreamRouteHolder = httpContext.Items.DownstreamRouteHolder();
        var route = downstreamRouteHolder.Route;
        var downstreamRoutes = route.DownstreamRoute;

        // case 1: if websocket request or single downstream route
        if (ShouldProcessSingleRoute(httpContext, downstreamRoutes))
        {
            await ProcessSingleRoute(httpContext, downstreamRoutes[0]);
            return;
        }

        // case 2: if no downstream routes
        if (downstreamRoutes.Count == 0)
        {
            return;
        }

        // case 3: if multiple downstream routes
        var routeKeysConfigs = route.DownstreamRouteConfig;
        if (routeKeysConfigs == null || routeKeysConfigs.Count == 0)
        {
            await ProcessRoutes(httpContext, route);
            return;
        }

        // case 4: if multiple downstream routes with route keys
        var mainResponseContext = await ProcessMainRoute(httpContext, downstreamRoutes[0]);
        if (mainResponseContext == null)
        {
            return;
        }

        var responsesContexts = await ProcessRoutesWithRouteKeys(httpContext, downstreamRoutes, routeKeysConfigs, mainResponseContext);
        if (responsesContexts.Length == 0)
        {
            return;
        }

        await MapResponses(httpContext, route, mainResponseContext, responsesContexts);
    }

    /// <summary>
    /// Helper method to determine if only the first downstream route should be processed.
    /// It is the case if the request is a websocket request or if there is only one downstream route.
    /// </summary>
    /// <param name="context">The http context.</param>
    /// <param name="routes">The downstream routes.</param>
    /// <returns>True if only the first downstream route should be processed.</returns>
    private static bool ShouldProcessSingleRoute(HttpContext context, ICollection routes) => context.WebSockets.IsWebSocketRequest || routes.Count == 1;

    /// <summary>
    /// Processing a single downstream route (no route keys).
    /// In that case, no need to make copies of the http context.
    /// </summary>
    /// <param name="context">The http context.</param>
    /// <param name="route">The downstream route.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    protected virtual Task ProcessSingleRoute(HttpContext context, DownstreamRoute route)
    {
        context.Items.UpsertDownstreamRoute(route);
        return _next.Invoke(context);
    }

    /// <summary>
    /// Processing the downstream routes (no route keys).
    /// </summary>
    /// <param name="context">The main http context.</param>
    /// <param name="route">The route.</param>
    private async Task ProcessRoutes(HttpContext context, Route route)
    {
        var tasks = route.DownstreamRoute.Select(downstreamRoute => ProcessRouteAsync(context, downstreamRoute))
            .ToArray();
        var contexts = await Task.WhenAll(tasks);
        await Map(context, route, [.. contexts]);
    }

    /// <summary>
    /// When using route keys, the first route is the main route and the rest are additional routes.
    /// Since we need to break if the main route response is null, we must process the main route first.
    /// </summary>
    /// <param name="context">The http context.</param>
    /// <param name="route">The first route, the main route.</param>
    /// <returns>The updated http context.</returns>
    private async Task<HttpContext> ProcessMainRoute(HttpContext context, DownstreamRoute route)
    {
        context.Items.UpsertDownstreamRoute(route);
        await _next.Invoke(context);
        return context;
    }

    /// <summary>
    /// Processing the downstream routes with route keys except the main route that has already been processed.
    /// </summary>
    /// <param name="context">The main http context.</param>
    /// <param name="routes">The downstream routes.</param>
    /// <param name="routeKeysConfigs">The route keys config.</param>
    /// <param name="mainResponse">The response from the main route.</param>
    /// <returns>A list of the tasks' http contexts.</returns>
    protected virtual async Task<HttpContext[]> ProcessRoutesWithRouteKeys(HttpContext context, IEnumerable<DownstreamRoute> routes, IReadOnlyCollection<AggregateRouteConfig> routeKeysConfigs, HttpContext mainResponse)
    {
        var routesProcessingList = new List<Task<HttpContext>>();
        var content = await mainResponse.Items.DownstreamResponse().Content.ReadAsStringAsync();
        var jObject = JToken.Parse(content);

        foreach (var downstreamRoute in routes.Skip(1))
        {
            var matchAdvancedAgg = routeKeysConfigs.FirstOrDefault(q => q.RouteKey == downstreamRoute.Key);
            if (matchAdvancedAgg != null)
            {
                routesProcessingList.AddRange(ProcessRouteWithComplexAggregation(matchAdvancedAgg, jObject, context, downstreamRoute));
                continue;
            }

            routesProcessingList.Add(ProcessRouteAsync(context, downstreamRoute));
        }

        return await Task.WhenAll(routesProcessingList);
    }

    /// <summary>
    /// Mapping responses.
    /// </summary>
    private Task MapResponses(HttpContext context, Route route, HttpContext mainResponseContext,
        IEnumerable<HttpContext> responsesContexts)
    {
        var contexts = new List<HttpContext> { mainResponseContext };
        contexts.AddRange(responsesContexts);
        return Map(context, route, contexts);
    }

    /// <summary>
    /// Processing a route with aggregation.
    /// </summary>
    private IEnumerable<Task<HttpContext>> ProcessRouteWithComplexAggregation(AggregateRouteConfig matchAdvancedAgg,
        JToken jObject, HttpContext httpContext, DownstreamRoute downstreamRoute)
    {
        var routesProcessingList = new List<Task<HttpContext>>();
        var values = jObject.SelectTokens(matchAdvancedAgg.JsonPath).Select(s => s.ToString()).Distinct();
        foreach (var value in values)
        {
            var tPnv = httpContext.Items.TemplatePlaceholderNameAndValues();
            tPnv.Add(new PlaceholderNameAndValue('{' + matchAdvancedAgg.Parameter + '}', value));
            routesProcessingList.Add(ProcessRouteAsync(httpContext, downstreamRoute, tPnv));
        }

        return routesProcessingList;
    }

    /// <summary>
    /// Process a downstream route asynchronously.
    /// </summary>
    /// <returns>The cloned Http context.</returns>
    private async Task<HttpContext> ProcessRouteAsync(HttpContext sourceContext, DownstreamRoute route, List<PlaceholderNameAndValue> placeholders = null)
    {
        var newHttpContext = CreateThreadContext(sourceContext);
        CopyItemsToNewContext(newHttpContext, sourceContext, placeholders);
        newHttpContext.Items.UpsertDownstreamRoute(route);

        await _next.Invoke(newHttpContext);
        return newHttpContext;
    }

    /// <summary>
    /// Copying some needed parameters to the Http context items.
    /// </summary>
    private static void CopyItemsToNewContext(HttpContext target, HttpContext source, List<PlaceholderNameAndValue> placeholders = null)
    {
        target.Items.Add(RequestIdString, source.Items[RequestIdString]);
        target.Items.SetIInternalConfiguration(source.Items.IInternalConfiguration());
        target.Items.UpsertTemplatePlaceholderNameAndValues(placeholders ??
                                                            source.Items.TemplatePlaceholderNameAndValues());
    }

    /// <summary>
    /// Creates a new HttpContext based on the source.
    /// </summary>
    /// <param name="source">The base http context.</param>
    /// <returns>The cloned context.</returns>
    private static HttpContext CreateThreadContext(HttpContext source)
    {
        var target = new DefaultHttpContext
        {
            Request =
            {
                Body = source.Request.Body,// TODO Consider stream cloning for multiple reads
                ContentLength = source.Request.ContentLength,
                ContentType = source.Request.ContentType,
                Host = source.Request.Host,
                Method = source.Request.Method,
                Path = source.Request.Path,
                PathBase = source.Request.PathBase,
                Protocol = source.Request.Protocol,
                QueryString = source.Request.QueryString,
                Scheme = source.Request.Scheme,
                IsHttps = source.Request.IsHttps,
                Query = new QueryCollection(new Dictionary<string, StringValues>(source.Request.Query)),
                RouteValues = new RouteValueDictionary(source.Request.RouteValues),
            },
            Connection =
            {
                RemoteIpAddress = source.Connection.RemoteIpAddress,
            },
            RequestServices = source.RequestServices,
            RequestAborted = source.RequestAborted,
            User = source.User,
        };

        foreach (var header in source.Request.Headers)
        {
            target.Request.Headers[header.Key] = header.Value.ToArray();
        }

        return target;
    }

    protected virtual Task Map(HttpContext httpContext, Route route, List<HttpContext> contexts)
    {
        if (route.DownstreamRoute.Count == 1)
        {
            return Task.CompletedTask;
        }

        var aggregator = _factory.Get(route);
        return aggregator.Aggregate(route, httpContext, contexts);
    }
}
