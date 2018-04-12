using System.Collections.Generic;
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
            await MapAggregateContent(originalContext, downstreamContexts);
        }

        private static async Task MapAggregateContent(DownstreamContext originalContext, List<DownstreamContext> downstreamContexts)
        {
            var contentBuilder = new StringBuilder();

            contentBuilder.Append("{");

            for (var i = 0; i < downstreamContexts.Count; i++)
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

            var stringContent = new StringContent(contentBuilder.ToString())
            {
                Headers = {ContentType = new MediaTypeHeaderValue("application/json")}
            };

            originalContext.DownstreamResponse = new DownstreamResponse(stringContent, HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>());
        }

        private static void MapAggregateError(DownstreamContext originalContext, List<DownstreamContext> downstreamContexts, int i)
        {
            originalContext.Errors.AddRange(downstreamContexts[i].Errors);
            originalContext.DownstreamResponse = downstreamContexts[i].DownstreamResponse;
        }
    }
}
