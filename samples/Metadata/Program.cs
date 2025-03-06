using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ocelot.Configuration;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Responder;
using Ocelot.Samples.Metadata;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddOcelot(); // single ocelot.json file in read-only mode
builder.Services
    .AddOcelot(builder.Configuration)
    .Services.RemoveAll<IHttpResponder>()
    .TryAddSingleton<IHttpResponder, MetadataContextResponder>();

if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddConsole();
}

var app = builder.Build();
//await app.UseOcelot();
var configuration = new OcelotPipelineConfiguration
{
    PreErrorResponderMiddleware = async (context, next) =>
    {
        // Get downstream response first
        await next.Invoke(); // ResponderMiddleware

        // Ensure the route has metadata at all
        var route = context.Items.DownstreamRoute();
        var metadata = route?.MetadataOptions.Metadata;
        // if ((metadata?.Count ?? 0) == 0)
        return;

        // Content type is 'application/json', so embed route metadata JSON-node
        var response = context.Items.DownstreamResponse(); // downstream response is already disposed
        if (response.Content.Headers.ContentType?.MediaType == "application/json")
        {
            //await using var jsonContent = await response.Content.ReadAsStreamAsync();
            //context.Response.Body.Seek(0L, SeekOrigin.Begin); // life-hack

            using var jsonStream = new MemoryStream();
            await response.Content.CopyToAsync(jsonStream, context.RequestAborted);
            //await content.CopyToAsync(context.Response.Body, context.RequestAborted);

            //await using var jsonContent = await context.Response.Body. ReadAsStreamAsync();
            var json1 = await JsonNode.ParseAsync(jsonStream, cancellationToken: context.RequestAborted);
            var json2 = JsonSerializer.SerializeToNode(metadata);

            // Assuming both are JsonObject
            //var jsonObject1 = json1.AsObject();
            //var jsonObject2 = json2.AsObject();
            var obj = new JsonObject
            {
                [nameof(HttpContext.Response)] = json1,
                [nameof(MetadataOptions.Metadata)] = json2,
            };

            var json = obj.ToJsonString();
            if (response.StatusCode != HttpStatusCode.NotModified && context.Response.ContentLength != 0)
            {
                //await using var content = await response.Content.ReadAsStreamAsync();
                context.Response.Body.Seek(0L, SeekOrigin.Begin); // life-hack
                //await content.CopyToAsync(context.Response.Body, context.RequestAborted);
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(json);
            }
        }
    },
    
};
await app.UseOcelot(configuration);
//app.UseMiddleware<MetadataMiddleware>();
await app.RunAsync();
