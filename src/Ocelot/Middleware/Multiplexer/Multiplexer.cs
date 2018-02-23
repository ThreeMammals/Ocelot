using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Ocelot.Configuration;

namespace Ocelot.Middleware.Multiplexer
{
    public interface IMultiplexer
    {
        Task Multiplex(DownstreamContext context, ReRoute reRoute, OcelotRequestDelegate next);
    }

    public class Multiplexer : IMultiplexer
    {
        public async Task Multiplex(DownstreamContext context, ReRoute reRoute, OcelotRequestDelegate next)
        {
            var tasks = new Task<DownstreamContext>[reRoute.DownstreamReRoute.Count];

            for (int i = 0; i < reRoute.DownstreamReRoute.Count; i++)
            {
                var downstreamContext = new DownstreamContext(context.HttpContext)
                {
                    TemplatePlaceholderNameAndValues = context.TemplatePlaceholderNameAndValues,
                    ServiceProviderConfiguration = context.ServiceProviderConfiguration,
                    DownstreamReRoute = reRoute.DownstreamReRoute[i],
                    //todo do we want these set here
                    RequestId = context.RequestId,
                    PreviousRequestId = context.PreviousRequestId,
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

            var aggregator = new SimpleResponseAggregator();
            await aggregator.Aggregate(reRoute, context, downstreamContexts);
        }

        private async Task<DownstreamContext> Fire(DownstreamContext context, OcelotRequestDelegate next)
        {
            await next.Invoke(context);
            return context;
        }
    }

    public class SimpleResponseAggregator
    {
        public async Task Aggregate(ReRoute reRoute, DownstreamContext originalContext, List<DownstreamContext> downstreamContexts)
        {
            if (reRoute.DownstreamReRoute.Count > 1)
            {
                var builder = new StringBuilder();

                foreach (var downstream in downstreamContexts)
                {
                    var content = await downstream.DownstreamResponse.Content.ReadAsStringAsync();
                    builder.Append($"{downstream.DownstreamReRoute.Key}:{content}\r\n");
                }

                originalContext.Errors = downstreamContexts[0].Errors;
                originalContext.DownstreamRequest = downstreamContexts[0].DownstreamRequest;
                originalContext.DownstreamResponse = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(builder.ToString())
                };
                originalContext.RequestId = downstreamContexts[0].RequestId;
                originalContext.PreviousRequestId = downstreamContexts[0].RequestId;
            }
            else
            {
                var finished = downstreamContexts[0];

                originalContext.Errors = finished.Errors;
                originalContext.DownstreamRequest = finished.DownstreamRequest;
                originalContext.DownstreamResponse = finished.DownstreamResponse;
                originalContext.RequestId = finished.RequestId;
                originalContext.PreviousRequestId = finished.RequestId;
            }
        }
    }
}
