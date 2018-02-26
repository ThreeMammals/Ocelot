using System.Collections.Generic;
using System.Threading.Tasks;
using Ocelot.Configuration;

namespace Ocelot.Middleware.Multiplexer
{
    public class Multiplexer : IMultiplexer
    {
        private readonly IResponseAggregator _aggregator;

        public Multiplexer(IResponseAggregator aggregator)
        {
            _aggregator = aggregator;
        }

        public async Task Multiplex(DownstreamContext context, ReRoute reRoute, OcelotRequestDelegate next)
        {
            var tasks = new Task<DownstreamContext>[reRoute.DownstreamReRoute.Count];

            for (var i = 0; i < reRoute.DownstreamReRoute.Count; i++)
            {
                var downstreamContext = new DownstreamContext(context.HttpContext)
                {
                    TemplatePlaceholderNameAndValues = context.TemplatePlaceholderNameAndValues,
                    ServiceProviderConfiguration = context.ServiceProviderConfiguration,
                    DownstreamReRoute = reRoute.DownstreamReRoute[i],
                };

                tasks[i] = Fire(downstreamContext, next);
            }

            await Task.WhenAll(tasks);

            var downstreamContexts = new List<DownstreamContext>();

            foreach (var task in tasks)
            {
                var finished = await task;
                downstreamContexts.Add(finished);
            }

            await _aggregator.Aggregate(reRoute, context, downstreamContexts);
        }

        private async Task<DownstreamContext> Fire(DownstreamContext context, OcelotRequestDelegate next)
        {
            await next.Invoke(context);
            return context;
        }
    }
}
