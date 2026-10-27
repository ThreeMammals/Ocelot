using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Middleware;
using System.Net.Http.Headers;
using System.Net.Mime;

namespace Ocelot.Multiplexer;

public class SimpleJsonResponseAggregator : IResponseAggregator
{
    public async Task Aggregate(Route route, HttpContext originalContext, List<HttpContext> downstreamContexts)
    {
        await MapAggregateContent(originalContext, downstreamContexts);
    }

    private static async Task MapAggregateContent(HttpContext originalContext, List<HttpContext> downstreamContexts)
    {
        var json = new StringBuilder("{");
        var responseKeys = downstreamContexts
            .Select(s => s.Items.DownstreamRoute().Key)
            .Distinct().ToArray();

        foreach (var key in responseKeys)
        {
            var contexts = downstreamContexts.Where(w => w.Items.DownstreamRoute().Key == key).ToArray();
            if (contexts.Length == 1)
            {
                var ctx = contexts[0];
                if (ctx.Items.Errors().Count > 0)
                {
                    MapAggregateError(originalContext, ctx);
                    return;
                }

                var drsp = ctx.Items.DownstreamResponse();
                var content = await drsp.Content.ReadAsStringAsync();
                json.Append($"\"{key}\":{content}"); // TODO Chain of Append instead of interpolated obj
            }
            else
            {
                json.Append($"\"{key}\":[");

                foreach (var ctxt in contexts)
                {
                    if (ctxt.Items.Errors().Count > 0)
                    {
                        MapAggregateError(originalContext, ctxt);
                        return;
                    }

                    var content = await ctxt.Items.DownstreamResponse().Content.ReadAsStringAsync();
                    if (string.IsNullOrWhiteSpace(content))
                        continue;

                    json.Append(content).Append(',');
                }
                json.Length--; // remove last comma ','
                json.Append(']');
            }

            json.Append(',');
        }
        json.Length--; // remove last comma ','
        json.Append('}');

        var jsonContent = new StringContent(json.ToString(), new MediaTypeHeaderValue(MediaTypeNames.Application.Json));
        var dr = new DownstreamResponse(jsonContent, HttpStatusCode.OK, EmptyHeaders, "cannot return from aggregate..which reason phrase would you use?");
        originalContext.Items.UpsertDownstreamResponse(dr);
    }

    private static readonly List<Header> EmptyHeaders = [];

    // TODO Make it virtual.
    private static void MapAggregateError(HttpContext originalContext, HttpContext downstreamContext)
    {
        originalContext.Items.UpsertErrors(downstreamContext.Items.Errors());
        originalContext.Items.UpsertDownstreamResponse(downstreamContext.Items.DownstreamResponse());
    }
}
