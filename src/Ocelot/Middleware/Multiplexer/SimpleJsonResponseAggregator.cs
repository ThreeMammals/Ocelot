using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Ocelot.Configuration;

namespace Ocelot.Middleware.Multiplexer
{
    public class SimpleJsonResponseAggregator : IResponseAggregator
    {
        public async Task Aggregate(ReRoute reRoute, DownstreamContext originalContext, List<DownstreamContext> downstreamContexts)
        {
            if (reRoute.DownstreamReRoute.Count > 1)
            {
                await MapAggregtes(originalContext, downstreamContexts);
            }
            else
            {
                MapNotAggregate(originalContext, downstreamContexts);
            }
        }

        private async Task MapAggregtes(DownstreamContext originalContext, List<DownstreamContext> downstreamContexts)
        {
            await MapAggregateContent(originalContext, downstreamContexts);
        }

        private static async Task MapAggregateContent(DownstreamContext originalContext, List<DownstreamContext> downstreamContexts)
        {
            var contentBuilder = new StringBuilder();

            contentBuilder.Append("{");

            for (int i = 0; i < downstreamContexts.Count; i++)
            {
                if (downstreamContexts[i].IsError)
                {
                    MapAggregateError(originalContext, downstreamContexts, i);
                    return;
                }

                var content = await downstreamContexts[i].DownstreamResponse.Content.ReadAsStringAsync();

                contentBuilder.Append($"\"{downstreamContexts[i].DownstreamReRoute.Key}\":{content}");

                if (i + 1 < downstreamContexts.Count)
                {
                    contentBuilder.Append(",");
                }
            }

            contentBuilder.Append("}");

            originalContext.DownstreamResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(contentBuilder.ToString())
                {
                    Headers = {ContentType = new MediaTypeHeaderValue("application/json")}
                }
            };
        }

        private static void MapAggregateError(DownstreamContext originalContext, List<DownstreamContext> downstreamContexts, int i)
        {
            originalContext.Errors.AddRange(downstreamContexts[i].Errors);
            originalContext.DownstreamResponse = downstreamContexts[i].DownstreamResponse;
        }

        private void MapNotAggregate(DownstreamContext originalContext, List<DownstreamContext> downstreamContexts)
        {
            //assume at least one..if this errors then it will be caught by global exception handler
            var finished = downstreamContexts.First();

            originalContext.Errors = finished.Errors;

            originalContext.DownstreamRequest = finished.DownstreamRequest;

            originalContext.DownstreamResponse = finished.DownstreamResponse;
        }
    }
}
