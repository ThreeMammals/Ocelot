using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Logging;
using Ocelot.Middleware;
using System.Collections;
using Route = Ocelot.Configuration.Route;

namespace Ocelot.Multiplexer;

public class MultiplexingMiddleware : OcelotMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IResponseAggregatorFactory _factory;
    private const string RequestIdString = "RequestId";

    public MultiplexingMiddleware(RequestDelegate next,
        IOcelotLoggerFactory loggerFactory,
        IResponseAggregatorFactory factory)
        : base(loggerFactory.CreateLogger<MultiplexingMiddleware>())
    {
        _factory = factory;
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var downstreamRouteHolder = context.Items.DownstreamRouteHolder();
        var route = downstreamRouteHolder.Route;
        var downstreamRoutes = route.DownstreamRoute;

        // Case 1: if websocket request or single downstream route
        if (ShouldProcessSingleRoute(context, downstreamRoutes))
        {
            await ProcessSingleRouteAsync(context, downstreamRoutes[0]);
            return;
        }

        // Case 2: if no downstream routes
        if (downstreamRoutes.Count == 0)
            return;

        // Case 3: if multiple downstream routes
        var routeKeysConfigs = route.DownstreamRouteConfig;
        if (routeKeysConfigs == null || routeKeysConfigs.Count == 0)
        {
            await ProcessRoutesAsync(context, route);
            return;
        }

        // Case 4: if multiple downstream routes with route keys
        var mainResponseContext = context;
        await ProcessMainRouteAsync(context, downstreamRoutes[0]);
        var responsesContexts = await ProcessRoutesWithRouteKeysAsync(context, downstreamRoutes, routeKeysConfigs, mainResponseContext);
        if (responsesContexts.Length == 0)
            return;

        await MapResponsesAsync(context, route, mainResponseContext, responsesContexts);
    }

    /// <summary>
    /// Helper method to determine if only the first downstream route should be processed.
    /// It is the case if the request is a websocket request or if there is only one downstream route.
    /// </summary>
    /// <param name="context">The http context.</param>
    /// <param name="routes">The downstream routes.</param>
    /// <returns>True if only the first downstream route should be processed.</returns>
    private static bool ShouldProcessSingleRoute(HttpContext context, ICollection routes)
        => context.WebSockets.IsWebSocketRequest || routes.Count == 1;

    /// <summary>
    /// Processing a single downstream route (no route keys).
    /// In that case, no need to make copies of the http context.
    /// </summary>
    /// <param name="context">The http context.</param>
    /// <param name="route">The downstream route.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    protected virtual Task ProcessSingleRouteAsync(HttpContext context, DownstreamRoute route)
    {
        context.Items.UpsertDownstreamRoute(route);
        return _next.Invoke(context);
    }

    /// <summary>
    /// Processing the downstream routes (no route keys).
    /// </summary>
    /// <param name="context">The main http context.</param>
    /// <param name="route">The route.</param>
    private async Task ProcessRoutesAsync(HttpContext context, Route route)
    {
        var tasks = route.DownstreamRoute
            .Select(downstreamRoute => ProcessRouteAsync(context, downstreamRoute))
            .ToArray();
        var contexts = await Task.WhenAll(tasks);
        await MapAsync(context, route, new(contexts));
    }

    /// <summary>
    /// When using route keys, the first route is the main route and the rest are additional routes.
    /// Since we need to break if the main route response is null, we must process the main route first.
    /// </summary>
    /// <param name="context">The http context.</param>
    /// <param name="route">The first route, the main route.</param>
    /// <returns>The updated http context.</returns>
    private Task ProcessMainRouteAsync(HttpContext context, DownstreamRoute route)
    {
        context.Items.UpsertDownstreamRoute(route);
        return _next.Invoke(context);
    }

    /// <summary>
    /// Processing the downstream routes with route keys except the main route that has already been processed.
    /// </summary>
    /// <param name="context">The main http context.</param>
    /// <param name="routes">The downstream routes.</param>
    /// <param name="routeKeysConfigs">The route keys config.</param>
    /// <param name="mainResponse">The response from the main route.</param>
    /// <returns>A list of the tasks' http contexts.</returns>
    protected virtual async Task<HttpContext[]> ProcessRoutesWithRouteKeysAsync(HttpContext context, IEnumerable<DownstreamRoute> routes, IReadOnlyCollection<AggregateRouteConfig> routeKeysConfigs, HttpContext mainResponse)
    {
        var processing = new List<Task<HttpContext>>();
        var content = await mainResponse.Items.DownstreamResponse().Content.ReadAsStringAsync();
        var jObject = JToken.Parse(content);

        foreach (var downstreamRoute in routes.Skip(1))
        {
            var matchAdvancedAgg = routeKeysConfigs.FirstOrDefault(q => q.RouteKey == downstreamRoute.Key);
            if (matchAdvancedAgg != null)
            {
                processing.AddRange(ProcessRouteWithComplexAggregation(matchAdvancedAgg, jObject, context, downstreamRoute));
            }
            else
            {
                processing.Add(ProcessRouteAsync(context, downstreamRoute));
            }
        }

        return await Task.WhenAll(processing);
    }

    /// <summary>
    /// Mapping responses.
    /// </summary>
    private Task MapResponsesAsync(HttpContext context, Route route, HttpContext mainResponseContext, IEnumerable<HttpContext> responsesContexts)
    {
        var contexts = new List<HttpContext> { mainResponseContext };
        contexts.AddRange(responsesContexts);
        return MapAsync(context, route, contexts);
    }

    /// <summary>
    /// Processing a route with aggregation.
    /// </summary>
    private List<Task<HttpContext>> ProcessRouteWithComplexAggregation(
        AggregateRouteConfig matchAdvancedAgg, JToken jObject, HttpContext context, DownstreamRoute route)
    {
        var processing = new List<Task<HttpContext>>();
        var values = jObject.SelectTokens(matchAdvancedAgg.JsonPath).Select(s => s.ToString()).Distinct();
        foreach (var value in values)
        {
            var tPnv = context.Items.TemplatePlaceholderNameAndValues();
            var lPnv = new List<PlaceholderNameAndValue>(tPnv)
            {
                new(matchAdvancedAgg.Parameter, value, true)
            };
            processing.Add(ProcessRouteAsync(context, route, lPnv));
        }

        return processing;
    }

    /// <summary>
    /// Process a downstream route asynchronously.
    /// </summary>
    /// <returns>The cloned Http context.</returns>
    private async Task<HttpContext> ProcessRouteAsync(HttpContext sourceContext, DownstreamRoute route, List<PlaceholderNameAndValue> placeholders = null)
    {
        var newHttpContext = await CreateThreadContextAsync(sourceContext, route);
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
        target.Items.UpsertTemplatePlaceholderNameAndValues(placeholders ?? source.Items.TemplatePlaceholderNameAndValues());
    }

    /// <summary>
    /// Creates a new HttpContext based on the source.
    /// </summary>
    /// <param name="source">The base http context.</param>
    /// <param name="route">Downstream route.</param>
    /// <returns>The cloned context.</returns>
    protected virtual async Task<HttpContext> CreateThreadContextAsync(HttpContext source, DownstreamRoute route)
    {
        var from = source.Request;
        var bodyStream = await CloneRequestBodyAsync(from, route, source.RequestAborted);
        var target = new DefaultHttpContext
        {
            Request =
            {
                Body = bodyStream,
                ContentLength = from.ContentLength,
                ContentType = from.ContentType,
                Host = from.Host,
                Method = from.Method,
                Path = from.Path,
                PathBase = from.PathBase,
                Protocol = from.Protocol,
                QueryString = from.QueryString,
                Scheme = from.Scheme,
                IsHttps = from.IsHttps,
                Query = new QueryCollection(new Dictionary<string, StringValues>(from.Query)),
                RouteValues = new(from.RouteValues),
            },
            Connection =
            {
                RemoteIpAddress = source.Connection.RemoteIpAddress,
            },
            RequestServices = source.RequestServices,
            RequestAborted = source.RequestAborted,
            User = source.User,
        };
        foreach (var header in from.Headers)
        {
            target.Request.Headers[header.Key] = header.Value.ToArray();
        }

        // Once the downstream request is completed and the downstream response has been read, the downstream response object can dispose of the body's Stream object
        target.Response.RegisterForDisposeAsync(bodyStream); // manage Stream lifetime by HttpResponse object
        return target;
    }

    public const string CurrentAggregateRouteKeyItem = "CurrentAggregateRouteKey";
    protected virtual Task MapAsync(HttpContext context, Route route, List<HttpContext> contexts)
    {
        if (route.DownstreamRoute.Count == 1)
            return Task.CompletedTask;

        // Ensure each context retains its correct aggregate key for proper response mapping
        if (route.DownstreamRouteConfig?.Count > 0)
        {
            for (int i = 0; i < contexts.Count && i < route.DownstreamRouteConfig.Count; i++)
            {
                var key = route.DownstreamRouteConfig[i].RouteKey;
                contexts[i].Items[CurrentAggregateRouteKeyItem] = key;
            }
        }

        var aggregator = _factory.Get(route);
        return aggregator.Aggregate(route, context, contexts);
    }

    protected virtual async Task<Stream> CloneRequestBodyAsync(HttpRequest request, DownstreamRoute route, CancellationToken aborted)
    {
        request.EnableBuffering();
        if (request.Body.Position != 0)
        {
            Logger.LogWarning(() => $"Ocelot does not support body copy without stream in initial position 0 for the route {route.Name()}.");
            return request.Body;
        }

        var targetBuffer = new MemoryStream();
        if (request.ContentLength is not null)
        {
            await request.Body.CopyToAsync(targetBuffer, (int)request.ContentLength, aborted);
            targetBuffer.Position = 0;
            request.Body.Position = 0;
        }
        else
        {
            Logger.LogInformation(() => $"Aggregation does not support body copy without Content-Length header, skipping body copy for the route {route.Name()}.");
        }

        return targetBuffer;
    }
}
