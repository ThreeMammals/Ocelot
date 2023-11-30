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
            await ProcessSingleRoute(httpContext, downstreamRoutes.First());
            return;
        }

        // case 2: if no downstream routes
        if (downstreamRoutes.Count == 0)
        {
            return;
        }

        // case 3: if multiple downstream routes
        var routeKeysConfigs = route.DownstreamRouteConfig;
        if (routeKeysConfigs == null || !routeKeysConfigs.Any())
        {
            await ProcessRoutes(httpContext, route);
            return;
        }

        // case 4: if multiple downstream routes with route keys
        var mainResponse = await ProcessMainRoute(httpContext, downstreamRoutes.First());
        if (mainResponse == null)
        {
            return;
        }

        var tasksList = await ProcessRoutesWithRouteKeys(httpContext, downstreamRoutes, routeKeysConfigs, mainResponse);
        if (!tasksList.Any())
        {
            return;
        }

        await MapResponses(httpContext, route, mainResponse, tasksList.ToList());
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
    private async Task ProcessSingleRoute(HttpContext context, DownstreamRoute route)
    {
        context.Items.UpsertDownstreamRoute(route);
        await _next.Invoke(context);
    }

    /// <summary>
    /// Processing the downstream routes (no route keys).
    /// </summary>
    /// <param name="context">The main http context.</param>
    /// <param name="route">The route.</param>
    private async Task ProcessRoutes(HttpContext context, Route route)
    {
        var tasks = route.DownstreamRoute.Select(downstreamRoute => ProcessRouteAsync(context, downstreamRoute, _next))
            .ToArray();
        var contexts = await Task.WhenAll(tasks);
        await Map(context, route, contexts.ToList());
    }

    /// <summary>
    /// When using route keys, the first route is the main route and the rest are additional routes.
    /// Since we need to break if the main route response is null, we need to process the main route first.
    /// </summary>
    /// <param name="context">The http context.</param>
    /// <param name="route">The first route, the main route.</param>
    /// <returns>The updated http context.</returns>
    private async Task<HttpContext> ProcessMainRoute(HttpContext context, DownstreamRoute route)
    {
        context.Items.UpsertDownstreamRoute(route);
        return await Fire(context, _next);
    }

    /// <summary>
    /// Processing the downstream routes with route keys except the main route that has already been processed.
    /// </summary>
    /// <param name="context">The main http context.</param>
    /// <param name="routes">The downstream routes.</param>
    /// <param name="routeKeysConfigs">The route keys config.</param>
    /// <param name="mainResponse">The response from the main route.</param>
    /// <returns>A list of the tasks' http contexts.</returns>
    private async Task<HttpContext[]> ProcessRoutesWithRouteKeys(HttpContext context,
        IEnumerable<DownstreamRoute> routes, IReadOnlyCollection<AggregateRouteConfig> routeKeysConfigs,
        HttpContext mainResponse)
    {
        var tasksList = new List<Task<HttpContext>>();
        var content = await mainResponse.Items.DownstreamResponse().Content.ReadAsStringAsync();
        var jObject = JToken.Parse(content);

        foreach (var downstreamRoute in routes.Skip(1))
        {
            var matchAdvancedAgg = routeKeysConfigs.FirstOrDefault(q => q.RouteKey == downstreamRoute.Key);
            if (matchAdvancedAgg != null)
            {
                tasksList.AddRange(ProcessRouteWithAggregation(matchAdvancedAgg, jObject, context, downstreamRoute));
                continue;
            }

            tasksList.Add(ProcessRouteAsync(context, downstreamRoute, _next));
        }

        return await Task.WhenAll(tasksList);
    }

    /// <summary>
    /// Mapping responses.
    /// </summary>
    private async Task MapResponses(HttpContext context, Route route, HttpContext mainResponse,
        IEnumerable<HttpContext> additionalResponses)
    {
        var contexts = new List<HttpContext> { mainResponse };
        contexts.AddRange(additionalResponses);
        await Map(context, route, contexts);
    }

    /// <summary>
    /// Processing a route with aggregation.
    /// </summary>
    private IEnumerable<Task<HttpContext>> ProcessRouteWithAggregation(AggregateRouteConfig matchAdvancedAgg,
        JToken jObject, HttpContext httpContext, DownstreamRoute downstreamRoute)
    {
        var tasks = new List<Task<HttpContext>>();
        var values = jObject.SelectTokens(matchAdvancedAgg.JsonPath).Select(s => s.ToString()).Distinct();
        foreach (var value in values)
        {
            var tPnv = httpContext.Items.TemplatePlaceholderNameAndValues();
            tPnv.Add(new PlaceholderNameAndValue('{' + matchAdvancedAgg.Parameter + '}', value));
            tasks.Add(ProcessRouteAsync(httpContext, downstreamRoute, _next, tPnv));
        }

        return tasks;
    }

    /// <summary>
    /// Process a downstream route asynchronously.
    /// </summary>
    /// <returns>The cloned Http context.</returns>
    private static async Task<HttpContext> ProcessRouteAsync(HttpContext sourceContext, DownstreamRoute route,
        RequestDelegate next, List<PlaceholderNameAndValue> placeholders = null)
    {
        var newHttpContext = CreateThreadContext(sourceContext);
        CopyItemsToNewContext(newHttpContext, sourceContext, placeholders);
        newHttpContext.Items.UpsertDownstreamRoute(route);

        return await Fire(newHttpContext, next);
    }

    /// <summary>
    /// Copying some needed parameters to the Http context items.
    /// </summary>
    private static void CopyItemsToNewContext(HttpContext target, HttpContext source,
        List<PlaceholderNameAndValue> placeholders = null)
    {
        target.Items.Add("RequestId", source.Items["RequestId"]);
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
        var target = new DefaultHttpContext();

        foreach (var header in source.Request.Headers)
        {
            target.Request.Headers[header.Key] = header.Value.ToArray();
        }

        target.Request.Body = source.Request.Body; // Consider stream cloning for multiple reads
        target.Request.ContentLength = source.Request.ContentLength;
        target.Request.ContentType = source.Request.ContentType;
        target.Request.Host = source.Request.Host;
        target.Request.Method = source.Request.Method;
        target.Request.Path = source.Request.Path;
        target.Request.PathBase = source.Request.PathBase;
        target.Request.Protocol = source.Request.Protocol;
        target.Request.QueryString = source.Request.QueryString;
        target.Request.Scheme = source.Request.Scheme;
        target.Request.IsHttps = source.Request.IsHttps;

        target.Request.Query = new QueryCollection(new Dictionary<string, StringValues>(source.Request.Query));
        target.Request.RouteValues = new RouteValueDictionary(source.Request.RouteValues);

        target.Connection.RemoteIpAddress = source.Connection.RemoteIpAddress;

        // Caution: Directly copying RequestServices might not be safe
        target.RequestServices = source.RequestServices;
        target.RequestAborted = source.RequestAborted;

        return target;
    }

    private async Task Map(HttpContext httpContext, Route route, List<HttpContext> contexts)
    {
        if (route.DownstreamRoute.Count == 1)
        {
            return;
        }

        var aggregator = _factory.Get(route);
        await aggregator.Aggregate(route, httpContext, contexts);
    }

    private static async Task<HttpContext> Fire(HttpContext httpContext, RequestDelegate next)
    {
        await next.Invoke(httpContext);
        return httpContext;
    }
}
