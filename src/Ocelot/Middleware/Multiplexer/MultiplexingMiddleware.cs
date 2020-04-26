namespace Ocelot.DownstreamRouteFinder.Middleware
{
    using Microsoft.AspNetCore.Http;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using Ocelot.Configuration;
    using Ocelot.DownstreamRouteFinder.UrlMatcher;
    using Ocelot.Logging;
    using Ocelot.Middleware;
    using Ocelot.Middleware.Multiplexer;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
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

        private HttpContext Copy(HttpContext source)
        {
            var target = new DefaultHttpContext();

            foreach (var header in source.Request.Headers)
            {
                target.Request.Headers.TryAdd(header.Key, header.Value);
            }

            target.Request.Body = source.Request.Body;
            target.Request.ContentLength = source.Request.ContentLength;
            target.Request.ContentType = source.Request.ContentType;
            target.Request.Host = source.Request.Host;
            target.Request.Method = source.Request.Method;
            target.Request.Path = source.Request.Path;
            target.Request.PathBase = source.Request.PathBase;
            target.Request.Protocol = source.Request.Protocol;
            target.Request.Query = source.Request.Query;
            target.Request.QueryString = source.Request.QueryString;
            target.Request.Scheme = source.Request.Scheme;
            target.Request.IsHttps = source.Request.IsHttps;
            target.Request.RouteValues = source.Request.RouteValues;
            target.Connection.RemoteIpAddress = source.Connection.RemoteIpAddress;
            target.RequestServices = source.RequestServices;

            target.Items.Add("RequestId", source.Items["RequestId"]);
            target.Items.SetIInternalConfiguration(source.Items.IInternalConfiguration());
            target.Items.SetTemplatePlaceholderNameAndValues(source.Items.TemplatePlaceholderNameAndValues());
            return target;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext.WebSockets.IsWebSocketRequest)
            {
                //todo this is obviously stupid
                httpContext.Items.SetDownstreamReRoute(httpContext.Items.DownstreamRoute().ReRoute.DownstreamReRoute[0]);
                await _next.Invoke(httpContext);
                return;
            }

            //var reRouteKeysConfigs = context.DownstreamRoute.ReRoute.DownstreamReRouteConfig;
            //if (reRouteKeysConfigs == null || !reRouteKeysConfigs.Any())
            //{

            var downstreamRoute = httpContext.Items.DownstreamRoute();

                var tasks = new Task<HttpContext>[downstreamRoute.ReRoute.DownstreamReRoute.Count];

                for (var i = 0; i < downstreamRoute.ReRoute.DownstreamReRoute.Count; i++)
                {
                    var newHttpContext = Copy(httpContext);
                    newHttpContext.Items.SetDownstreamReRoute(downstreamRoute.ReRoute.DownstreamReRoute[i]);
                    tasks[i] = Fire(newHttpContext, _next);
                }

                await Task.WhenAll(tasks);

                var contexts = new List<HttpContext>();

                foreach (var task in tasks)
                {
                    var finished = await task;
                    contexts.Add(finished);
                }

                await Map(httpContext, downstreamRoute.ReRoute, contexts);
            //}
            //else
            //{
            //    var downstreamContextMain = new DownstreamContext()
            //    {
            //        TemplatePlaceholderNameAndValues = context.TemplatePlaceholderNameAndValues,
            //        Configuration = context.Configuration,
            //        DownstreamReRoute = context.DownstreamRoute.ReRoute.DownstreamReRoute[0],
            //    };

            //    var mainResponse = await Fire(httpContext, _next);

            //    if (context.DownstreamRoute.ReRoute.DownstreamReRoute.Count == 1)
            //    {
            //        MapNotAggregate(context, httpContext, new List<HttpContext>() { mainResponse });
            //        return;
            //    }

            //    var tasks = new List<Task<HttpContext>>();
            //    if (mainResponse.Items.DownstreamResponse() == null)
            //    {
            //        return;
            //    }

            //    var content = await mainResponse.Items.DownstreamResponse().Content.ReadAsStringAsync();
            //    var jObject = Newtonsoft.Json.Linq.JToken.Parse(content);

            //    for (var i = 1; i < context.DownstreamRoute.ReRoute.DownstreamReRoute.Count; i++)
            //    {
            //        var templatePlaceholderNameAndValues = context.TemplatePlaceholderNameAndValues;
            //        var downstreamReRoute = context.DownstreamRoute.ReRoute.DownstreamReRoute[i];
            //        var matchAdvancedAgg = reRouteKeysConfigs.FirstOrDefault(q => q.ReRouteKey == downstreamReRoute.Key);
            //        if (matchAdvancedAgg != null)
            //        {
            //            var values = jObject.SelectTokens(matchAdvancedAgg.JsonPath).Select(s => s.ToString()).Distinct().ToList();

            //            foreach (var value in values)
            //            {
            //                var downstreamContext = new DownstreamContext()
            //                {
            //                    TemplatePlaceholderNameAndValues = new List<PlaceholderNameAndValue>(templatePlaceholderNameAndValues),
            //                    Configuration = context.Configuration,
            //                    DownstreamReRoute = downstreamReRoute,
            //                };
            //                downstreamContext.TemplatePlaceholderNameAndValues.Add(new PlaceholderNameAndValue("{" + matchAdvancedAgg.Parameter + "}", value.ToString()));
            //                tasks.Add(Fire(httpContext, _next));
            //            }
            //        }
            //        else
            //        {
            //            var downstreamContext = new DownstreamContext()
            //            {
            //                TemplatePlaceholderNameAndValues = new List<PlaceholderNameAndValue>(templatePlaceholderNameAndValues),
            //                Configuration = context.Configuration,
            //                DownstreamReRoute = downstreamReRoute,
            //            };
            //            tasks.Add(Fire(httpContext, _next));
            //        }
            //    }

            //    await Task.WhenAll(tasks);

            //    var contexts = new List<HttpContext>() { mainResponse };

            //    foreach (var task in tasks)
            //    {
            //        var finished = await task;
            //        contexts.Add(finished);
            //    }

            //    await Map(httpContext, context.DownstreamRoute.ReRoute, context, contexts);
            //}
        }

        private async Task Map(HttpContext httpContext, ReRoute reRoute, List<HttpContext> contexts)
        {
            if (reRoute.DownstreamReRoute.Count > 1)
            {
                var aggregator = _factory.Get(reRoute);
                //await aggregator.Aggregate(reRoute, context, contexts);
            }
            else
            {
                MapNotAggregate(httpContext, contexts);
            }
        }

        private void MapNotAggregate(HttpContext httpContext, List<HttpContext> downstreamContexts)
        {
            //assume at least one..if this errors then it will be caught by global exception handler
            var finished = downstreamContexts.First();

            httpContext.Items.SetErrors(finished.Items.Errors());

            httpContext.Items.SetDownstreamRequest(finished.Items.DownstreamRequest());

            httpContext.Items.SetDownstreamResponse(finished.Items.DownstreamResponse());

            //httpContext.Response.StatusCode = finished.Response.StatusCode;
            //httpContext.Response.Body = finished.Response.Body;
        }

        private async Task<HttpContext> Fire(HttpContext httpContext, RequestDelegate next)
        {
            //todo this wont work
            await next.Invoke(httpContext);
            return httpContext;
        }
    }
}
