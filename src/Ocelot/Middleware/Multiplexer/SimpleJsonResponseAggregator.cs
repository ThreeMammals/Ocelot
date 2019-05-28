using Ocelot.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

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

            var responseKeys = downstreamContexts.Select(s => s.DownstreamReRoute.Key).Distinct().ToList();

            for (var k = 0; k < responseKeys.Count; k++)
            {
                var contexts = downstreamContexts.Where(w => w.DownstreamReRoute.Key == responseKeys[k]).ToList();
                if (contexts.Count == 1)
                {
                    if (contexts[0].IsError)
                    {
                        MapAggregateError(originalContext, contexts[0]);
                        return;
                    }

                    var content = await contexts[0].DownstreamResponse.Content.ReadAsStringAsync();
                    contentBuilder.Append($"\"{responseKeys[k]}\":{content}");
                }
                else
                {
                    contentBuilder.Append($"\"{responseKeys[k]}\":");
                    contentBuilder.Append("[");

                    for (var i = 0; i < contexts.Count; i++)
                    {
                        if (contexts[i].IsError)
                        {
                            MapAggregateError(originalContext, contexts[i]);
                            return;
                        }

                        var content = await contexts[i].DownstreamResponse.Content.ReadAsStringAsync();
                        if (string.IsNullOrWhiteSpace(content))
                        {
                            continue;
                        }

                        contentBuilder.Append($"{content}");

                        if (i + 1 < contexts.Count)
                        {
                            contentBuilder.Append(",");
                        }
                    }

                    contentBuilder.Append("]");
                }

                if (k + 1 < responseKeys.Count)
                {
                    contentBuilder.Append(",");
                }
            }

            contentBuilder.Append("}");

            var stringContent = new StringContent(contentBuilder.ToString())
            {
                Headers = { ContentType = new MediaTypeHeaderValue("application/json") }
            };

            originalContext.DownstreamResponse = new DownstreamResponse(stringContent, HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>(), "cannot return from aggregate..which reason phrase would you use?");
        }

        private static void MapAggregateError(DownstreamContext originalContext, DownstreamContext downstreamContext)
        {
            originalContext.Errors.AddRange(downstreamContext.Errors);
            originalContext.DownstreamResponse = downstreamContext.DownstreamResponse;
        }
    }
}
