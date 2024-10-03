using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Logging;
using Ocelot.Middleware;
using System.Collections;
using System.Text.Json;
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

    public async Task Invoke(HttpContext httpContext)
    {
        var downstreamRouteHolder = httpContext.Items.DownstreamRouteHolder();
        var route = downstreamRouteHolder.Route;
        var downstreamRoutes = route.DownstreamRoute;

        // Case 1: if websocket request or single downstream route
        if (ShouldProcessSingleRoute(httpContext, downstreamRoutes))
        {
            await ProcessSingleRouteAsync(httpContext, downstreamRoutes[0]);
            return;
        }

        // Case 2: if no downstream routes
        if (downstreamRoutes.Count == 0)
        {
            return;
        }

        // Case 3: if multiple downstream routes
        var routeKeysConfigs = route.DownstreamRouteConfig;
        if (routeKeysConfigs == null || routeKeysConfigs.Count == 0)
        {
            await ProcessRoutesAsync(httpContext, route);
            return;
        }

        // Case 4: if multiple downstream routes with route keys
        var mainResponseContext = await ProcessMainRouteAsync(httpContext, downstreamRoutes[0]);
        if (mainResponseContext == null)
        {
            return;
        }

        var responsesContexts = await ProcessRoutesWithRouteKeysAsync(httpContext, downstreamRoutes, routeKeysConfigs, mainResponseContext);
        if (responsesContexts.Length == 0)
        {
            return;
        }

        await MapResponsesAsync(httpContext, route, mainResponseContext, responsesContexts);
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
    private async Task<HttpContext> ProcessMainRouteAsync(HttpContext context, DownstreamRoute route)
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
    protected virtual async Task<HttpContext[]> ProcessRoutesWithRouteKeysAsync(HttpContext context, IEnumerable<DownstreamRoute> routes, IReadOnlyCollection<AggregateRouteConfig> routeKeysConfigs, HttpContext mainResponse)
    {
        var processing = new List<Task<HttpContext>>();
        var content = await mainResponse.Items.DownstreamResponse().Content.ReadAsStringAsync();


        foreach (var downstreamRoute in routes.Skip(1))
        {
            var matchAdvancedAgg = routeKeysConfigs.FirstOrDefault(q => q.RouteKey == downstreamRoute.Key);
            if (matchAdvancedAgg != null)
            {
                processing.AddRange(ProcessRouteWithComplexAggregation(matchAdvancedAgg, content, context, downstreamRoute));
                continue;
            }

            processing.Add(ProcessRouteAsync(context, downstreamRoute));
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
    private IEnumerable<Task<HttpContext>> ProcessRouteWithComplexAggregation(AggregateRouteConfig matchAdvancedAgg
        , string content
        , HttpContext httpContext, DownstreamRoute downstreamRoute)
    {
        var processing = new List<Task<HttpContext>>();

        var values = ExtractValuesFromJsonPath(content, matchAdvancedAgg.JsonPath).Distinct();

        foreach (var value in values)
        {
            var tPnv = httpContext.Items.TemplatePlaceholderNameAndValues();
            tPnv.Add(new PlaceholderNameAndValue('{' + matchAdvancedAgg.Parameter + '}', value));
            processing.Add(ProcessRouteAsync(httpContext, downstreamRoute, tPnv));
        }

        return processing;
    }

    //public static IEnumerable<string> ExtractValuesFromJsonPath(string jsonContent, string jsonPath)
    //{
    //    using (JsonDocument document = JsonDocument.Parse(jsonContent))
    //    {
    //        var root = document.RootElement;

    //        // حذف '$' از مسیر و تقسیم آن بر اساس '.'
    //        var pathParts = jsonPath.Trim('$', '.').Split('.');

    //        var elements = new List<string>();

    //        // فراخوانی تابع بازگشتی برای پیمایش مسیر
    //        TraverseJsonPath(root, pathParts, 0, elements);

    //        return elements;
    //    }
    //}

    //// تابع بازگشتی برای پیمایش مسیر JSONPath
    //public static void TraverseJsonPath(JsonElement currentElement, string[] pathParts, int index, List<string> elements)
    //{
    //    if (index >= pathParts.Length) return;  // در صورت اتمام مسیر، از تابع خارج می‌شویم

    //    var part = pathParts[index];

    //    if (currentElement.ValueKind == JsonValueKind.Array)
    //    {
    //        // اگر آرایه باشد، برای هر عنصر در آرایه پیمایش می‌کنیم
    //        foreach (var element in currentElement.EnumerateArray())
    //        {
    //            TraverseJsonPath(element, pathParts, index + 1, elements);
    //        }
    //    }
    //    else if (currentElement.ValueKind == JsonValueKind.Object)
    //    {
    //        // اگر شیء باشد، ویژگی مورد نظر را می‌یابیم
    //        if (currentElement.TryGetProperty(part, out JsonElement nextElement))
    //        {
    //            TraverseJsonPath(nextElement, pathParts, index + 1, elements);
    //        }
    //    }
    //    else
    //    {
    //        // اگر عنصر از نوع دیگری باشد (مثلاً رشته یا عدد)، به نتیجه اضافه می‌کنیم
    //        elements.Add(currentElement.ToString());
    //    }
    //}

    public static IEnumerable<string> ExtractValuesFromJsonPathOld(string jsonContent, string jsonPath)
    {
        using (JsonDocument document = JsonDocument.Parse(jsonContent))
        {
            var root = document.RootElement;

            var pathParts = jsonPath.Trim('$', '.').Split('.');

            var elements = new List<string>();

            foreach (var part in pathParts)
            {
                if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in root.EnumerateArray())
                    {
                        if (element.ValueKind == JsonValueKind.Object)
                        {
                            if (element.TryGetProperty(part, out JsonElement property))
                            {
                                elements.Add(property.ToString());
                            }
                        }
                    }
                }
                else
                {
                    if (root.TryGetProperty(part, out JsonElement nextElement))
                    {
                        elements.Add(nextElement.ToString());
                    }
                    else
                    {
                        return Enumerable.Empty<string>();
                    }
                }
            }

            return elements;
        }
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
        target.Items.UpsertTemplatePlaceholderNameAndValues(placeholders ??
                                                            source.Items.TemplatePlaceholderNameAndValues());
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

    protected virtual Task MapAsync(HttpContext httpContext, Route route, List<HttpContext> contexts)
    {
        if (route.DownstreamRoute.Count == 1)
        {
            return Task.CompletedTask;
        }

        var aggregator = _factory.Get(route);
        return aggregator.Aggregate(route, httpContext, contexts);
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
