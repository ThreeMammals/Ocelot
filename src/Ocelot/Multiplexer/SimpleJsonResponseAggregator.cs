using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Middleware;
using System.Net.Http.Headers;

namespace Ocelot.Multiplexer
{
    public class SimpleJsonResponseAggregator : IResponseAggregator
    {
        public async Task Aggregate(Route route, HttpContext originalContext, List<HttpContext> downstreamContexts)
        {
            await MapAggregateContent(originalContext, downstreamContexts);
        }

        private static async Task MapAggregateContent(HttpContext originalContext, List<HttpContext> downstreamContexts)
        {
            var contentBuilder = new StringBuilder();

            contentBuilder.Append('{');

            var responseKeys = downstreamContexts.Select(s => s.Items.DownstreamRoute().Key).Distinct().ToArray();

            for (var k = 0; k < responseKeys.Length; k++)
            {
                var contexts = downstreamContexts.Where(w => w.Items.DownstreamRoute().Key == responseKeys[k]).ToArray();
                if (contexts.Length == 1)
                {
                    if (contexts[0].Items.Errors().Count > 0)
                    {
                        MapAggregateError(originalContext, contexts[0]);
                        return;
                    }

                    var content = await contexts[0].Items.DownstreamResponse().Content.ReadAsStringAsync();
                    contentBuilder.Append($"\"{responseKeys[k]}\":{content}");
                }
                else
                {
                    contentBuilder.Append($"\"{responseKeys[k]}\":");
                    contentBuilder.Append('[');

                    for (var i = 0; i < contexts.Length; i++)
                    {
                        if (contexts[i].Items.Errors().Count > 0)
                        {
                            MapAggregateError(originalContext, contexts[i]);
                            return;
                        }

                        var content = await contexts[i].Items.DownstreamResponse().Content.ReadAsStringAsync();
                        if (string.IsNullOrWhiteSpace(content))
                        {
                            continue;
                        }

                        contentBuilder.Append($"{content}");

                        if (i + 1 < contexts.Length)
                        {
                            contentBuilder.Append(',');
                        }
                    }

                    contentBuilder.Append(']');
                }

                if (k + 1 < responseKeys.Length)
                {
                    contentBuilder.Append(',');
                }
            }

            contentBuilder.Append('}');

            var stringContent = new StringContent(contentBuilder.ToString())
            {
                Headers = { ContentType = new MediaTypeHeaderValue("application/json") },
            };

            originalContext.Items.UpsertDownstreamResponse(new DownstreamResponse(stringContent, HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>(), "cannot return from aggregate..which reason phrase would you use?"));
        }

        private static void MapAggregateError(HttpContext originalContext, HttpContext downstreamContext)
        {
            originalContext.Items.UpsertErrors(downstreamContext.Items.Errors());
            originalContext.Items.UpsertDownstreamResponse(downstreamContext.Items.DownstreamResponse());
        }
    }
}
