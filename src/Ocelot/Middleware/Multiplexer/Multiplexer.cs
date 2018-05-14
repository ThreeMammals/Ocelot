using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ocelot.Configuration;

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
