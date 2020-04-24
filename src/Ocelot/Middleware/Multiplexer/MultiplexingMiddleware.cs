namespace Ocelot.DownstreamRouteFinder.Middleware
{
    using Microsoft.AspNetCore.Http;
    using Ocelot.DownstreamRouteFinder.Finder;
    using Ocelot.DownstreamRouteFinder.UrlMatcher;
    using Ocelot.Infrastructure.Extensions;
    using Ocelot.Logging;
    using Ocelot.Middleware;
    using Ocelot.Middleware.Multiplexer;
    using Ocelot.Request.Middleware;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class MultiplexingMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IResponseAggregatorFactory _factory;

        public MultiplexingMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IResponseAggregatorFactory factory
            )
                : base(loggerFactory.CreateLogger<DownstreamRouteFinderMiddleware>())
        {
            _factory = factory;
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext, IDownstreamContext context)
        {
            var reRouteKeysConfigs = context.DownstreamRoute.ReRoute.DownstreamReRouteConfig;
            if (reRouteKeysConfigs == null || !reRouteKeysConfigs.Any())
            {
                var tasks = new Task<HttpContext>[context.DownstreamRoute.ReRoute.DownstreamReRoute.Count];

                for (var i = 0; i < context.DownstreamRoute.ReRoute.DownstreamReRoute.Count; i++)
                {
                    var downstreamContext = new DownstreamContext
                    {
                        TemplatePlaceholderNameAndValues = context.TemplatePlaceholderNameAndValues,
                        Configuration = context.Configuration,
                        DownstreamReRoute = context.DownstreamRoute.ReRoute.DownstreamReRoute[i],
                    };

                    var newHttpContext = new DefaultHttpContext(httpContext.Features);

                    foreach (var header in httpContext.Request.Headers)
                    {
                        newHttpContext.Request.Headers.TryAdd(header.Key, header.Value);
                    }

                    newHttpContext.Request.Body = httpContext.Request.Body;
                    newHttpContext.Request.ContentLength = httpContext.Request.ContentLength;
                    newHttpContext.Request.ContentType = httpContext.Request.ContentType;
                    newHttpContext.Request.Host = httpContext.Request.Host;
                    newHttpContext.Request.Method = httpContext.Request.Method;
                    newHttpContext.Request.Path = httpContext.Request.Path;
                    newHttpContext.Request.PathBase = httpContext.Request.PathBase;
                    newHttpContext.Request.Protocol = httpContext.Request.Protocol;
                    newHttpContext.Request.Query = httpContext.Request.Query;
                    newHttpContext.Request.QueryString = httpContext.Request.QueryString;
                    newHttpContext.Request.Scheme = httpContext.Request.Scheme;
                    newHttpContext.Request.IsHttps = httpContext.Request.IsHttps;
                    newHttpContext.Request.RouteValues = httpContext.Request.RouteValues;
                    newHttpContext.Connection.RemoteIpAddress = httpContext.Connection.RemoteIpAddress;
                    //newHttpContext.Request.Form = httpContext.Request.Form;

                    // add the downstream re route to this context so we know what to work with in later
                    newHttpContext.Items.Add("DownstreamReRoute", context.DownstreamRoute.ReRoute.DownstreamReRoute[i]);

                    tasks[i] = Fire(newHttpContext, _next);
                }

                await Task.WhenAll(tasks);

                var contexts = new List<HttpContext>();

                foreach (var task in tasks)
                {
                    var finished = await task;
                    contexts.Add(finished);
                }

                await Map(context.DownstreamRoute.ReRoute, context, contexts);
            }
            else
            {
                var downstreamContextMain = new DownstreamContext()
                {
                    TemplatePlaceholderNameAndValues = context.TemplatePlaceholderNameAndValues,
                    Configuration = context.Configuration,
                    DownstreamReRoute = context.DownstreamRoute.ReRoute.DownstreamReRoute[0],
                };
                var mainResponse = await Fire(httpContext, _next);

                if (context.DownstreamRoute.ReRoute.DownstreamReRoute.Count == 1)
                {
                    MapNotAggregate(context, new List<HttpContext>() { mainResponse });
                    return;
                }

                var tasks = new List<Task<HttpContext>>();
                if (mainResponse.Items.DownstreamResponse() == null)
                {
                    return;
                }

                var content = await mainResponse.Items.DownstreamResponse().Content.ReadAsStringAsync();
                var jObject = Newtonsoft.Json.Linq.JToken.Parse(content);

                for (var i = 1; i < context.DownstreamRoute.ReRoute.DownstreamReRoute.Count; i++)
                {
                    var templatePlaceholderNameAndValues = context.TemplatePlaceholderNameAndValues;
                    var downstreamReRoute = context.DownstreamRoute.ReRoute.DownstreamReRoute[i];
                    var matchAdvancedAgg = reRouteKeysConfigs.FirstOrDefault(q => q.ReRouteKey == downstreamReRoute.Key);
                    if (matchAdvancedAgg != null)
                    {
                        var values = jObject.SelectTokens(matchAdvancedAgg.JsonPath).Select(s => s.ToString()).Distinct().ToList();

                        foreach (var value in values)
                        {
                            var downstreamContext = new DownstreamContext()
                            {
                                TemplatePlaceholderNameAndValues = new List<PlaceholderNameAndValue>(templatePlaceholderNameAndValues),
                                Configuration = context.Configuration,
                                DownstreamReRoute = downstreamReRoute,
                            };
                            downstreamContext.TemplatePlaceholderNameAndValues.Add(new PlaceholderNameAndValue("{" + matchAdvancedAgg.Parameter + "}", value.ToString()));
                            tasks.Add(Fire(httpContext, _next));
                        }
                    }
                    else
                    {
                        var downstreamContext = new DownstreamContext()
                        {
                            TemplatePlaceholderNameAndValues = new List<PlaceholderNameAndValue>(templatePlaceholderNameAndValues),
                            Configuration = context.Configuration,
                            DownstreamReRoute = downstreamReRoute,
                        };
                        tasks.Add(Fire(httpContext, _next));
                    }
                }

                await Task.WhenAll(tasks);

                var contexts = new List<HttpContext>() { mainResponse };

                foreach (var task in tasks)
                {
                    var finished = await task;
                    contexts.Add(finished);
                }

                await Map(context.DownstreamRoute.ReRoute, context, contexts);
            }
        }

        private async Task Map(Ocelot.Configuration.ReRoute reRoute, IDownstreamContext context, List<HttpContext> contexts)
        {
            if (reRoute.DownstreamReRoute.Count > 1)
            {
                var aggregator = _factory.Get(reRoute);
                //await aggregator.Aggregate(reRoute, context, contexts);
            }
            else
            {
                MapNotAggregate(context, contexts);
            }
        }

        private void MapNotAggregate(IDownstreamContext originalContext, List<HttpContext> downstreamContexts)
        {
            //assume at least one..if this errors then it will be caught by global exception handler
            var finished = downstreamContexts.First();

            //originalContext.Errors.AddRange(finished.Errors);

            originalContext.DownstreamRequest = finished.Items.DownstreamRequest();

            originalContext.DownstreamResponse = finished.Items.DownstreamResponse();
        }

        private async Task<HttpContext> Fire(HttpContext httpContext, RequestDelegate next)
        {
            //todo this wont work
            await next.Invoke(httpContext);
            return httpContext;
        }
    }

    public static class HttpItemsExtensions
    {
        public static void SetDownstreamRequest(this IDictionary<object, object> input, DownstreamRequest downstreamRequest)
        {
            input.Set("DownstreamRequest", downstreamRequest);
        }

        public static void SetDownstreamResponse(this IDictionary<object, object> input, DownstreamResponse downstreamResponse)
        {
            input.Set("DownstreamResponse", downstreamResponse);
        }

        public static DownstreamRequest DownstreamRequest(this IDictionary<object, object> input)
        {
            return input.Get<DownstreamRequest>("DownstreamRequest");
        }

        public static DownstreamResponse DownstreamResponse(this IDictionary<object, object> input)
        {
            return input.Get<DownstreamResponse>("DownstreamResponse");
        }

        private static T Get<T>(this IDictionary<object, object> input, string key)
        {
            if (input.TryGetValue(key, out var value))
            {
                return (T)value;
            }

            return default(T);
        }

        private static void Set<T>(this IDictionary<object, object> input, string key, T value)
        {
            input.Add(key, value);
        }
    }
}
