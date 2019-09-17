using Ocelot.Configuration;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ocelot.Middleware.Multiplexer
{
    public class Multiplexer : IMultiplexer
    {
        private readonly IResponseAggregatorFactory _factory;

        public Multiplexer(IResponseAggregatorFactory factory)
        {
            _factory = factory;
        }

        public async Task Multiplex(DownstreamContext context, ReRoute reRoute, OcelotRequestDelegate next)
        {
            var reRouteKeysConfigs = reRoute.DownstreamReRouteConfig;
            if (reRouteKeysConfigs == null || !reRouteKeysConfigs.Any())
            {
                var tasks = new Task<DownstreamContext>[reRoute.DownstreamReRoute.Count];

                for (var i = 0; i < reRoute.DownstreamReRoute.Count; i++)
                {
                    var downstreamContext = new DownstreamContext(context.HttpContext)
                    {
                        TemplatePlaceholderNameAndValues = context.TemplatePlaceholderNameAndValues,
                        Configuration = context.Configuration,
                        DownstreamReRoute = reRoute.DownstreamReRoute[i],
                    };

                    tasks[i] = Fire(downstreamContext, next);
                }

                await Task.WhenAll(tasks);

                var contexts = new List<DownstreamContext>();

                foreach (var task in tasks)
                {
                    var finished = await task;
                    contexts.Add(finished);
                }

                await Map(reRoute, context, contexts);
            }
            else
            {
                var downstreamContextMain = new DownstreamContext(context.HttpContext)
                {
                    TemplatePlaceholderNameAndValues = context.TemplatePlaceholderNameAndValues,
                    Configuration = context.Configuration,
                    DownstreamReRoute = reRoute.DownstreamReRoute[0],
                };
                var mainResponse = await Fire(downstreamContextMain, next);

                if (reRoute.DownstreamReRoute.Count == 1)
                {
                    MapNotAggregate(context, new List<DownstreamContext>() { mainResponse });
                    return;
                }

                var tasks = new List<Task<DownstreamContext>>();
                if (mainResponse.DownstreamResponse == null)
                {
                    return;
                }

                var content = await mainResponse.DownstreamResponse.Content.ReadAsStringAsync();
                var jObject = Newtonsoft.Json.Linq.JToken.Parse(content);

                for (var i = 1; i < reRoute.DownstreamReRoute.Count; i++)
                {
                    var templatePlaceholderNameAndValues = context.TemplatePlaceholderNameAndValues;
                    var downstreamReRoute = reRoute.DownstreamReRoute[i];
                    var matchAdvancedAgg = reRouteKeysConfigs.FirstOrDefault(q => q.ReRouteKey == downstreamReRoute.Key);
                    if (matchAdvancedAgg != null)
                    {
                        var values = jObject.SelectTokens(matchAdvancedAgg.JsonPath).Select(s => s.ToString()).Distinct().ToList();

                        foreach (var value in values)
                        {
                            var downstreamContext = new DownstreamContext(context.HttpContext)
                            {
                                TemplatePlaceholderNameAndValues = new List<PlaceholderNameAndValue>(templatePlaceholderNameAndValues),
                                Configuration = context.Configuration,
                                DownstreamReRoute = downstreamReRoute,
                            };
                            downstreamContext.TemplatePlaceholderNameAndValues.Add(new PlaceholderNameAndValue("{" + matchAdvancedAgg.Parameter + "}", value.ToString()));
                            tasks.Add(Fire(downstreamContext, next));
                        }
                    }
                    else
                    {
                        var downstreamContext = new DownstreamContext(context.HttpContext)
                        {
                            TemplatePlaceholderNameAndValues = new List<PlaceholderNameAndValue>(templatePlaceholderNameAndValues),
                            Configuration = context.Configuration,
                            DownstreamReRoute = downstreamReRoute,
                        };
                        tasks.Add(Fire(downstreamContext, next));
                    }
                }

                await Task.WhenAll(tasks);

                var contexts = new List<DownstreamContext>() { mainResponse };

                foreach (var task in tasks)
                {
                    var finished = await task;
                    contexts.Add(finished);
                }

                await Map(reRoute, context, contexts);
            }
        }

        private async Task Map(ReRoute reRoute, DownstreamContext context, List<DownstreamContext> contexts)
        {
            if (reRoute.DownstreamReRoute.Count > 1)
            {
                var aggregator = _factory.Get(reRoute);
                await aggregator.Aggregate(reRoute, context, contexts);
            }
            else
            {
                MapNotAggregate(context, contexts);
            }
        }

        private void MapNotAggregate(DownstreamContext originalContext, List<DownstreamContext> downstreamContexts)
        {
            //assume at least one..if this errors then it will be caught by global exception handler
            var finished = downstreamContexts.First();

            originalContext.Errors.AddRange(finished.Errors);

            originalContext.DownstreamRequest = finished.DownstreamRequest;

            originalContext.DownstreamResponse = finished.DownstreamResponse;
        }

        private async Task<DownstreamContext> Fire(DownstreamContext context, OcelotRequestDelegate next)
        {
            await next.Invoke(context);
            return context;
        }
    }
}
