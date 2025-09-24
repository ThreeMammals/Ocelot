using Microsoft.Extensions.DependencyInjection.Extensions;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Responder;
using Ocelot.Samples.Metadata;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddOcelot();
builder.Services
    .AddOcelot(builder.Configuration)
    .Services.RemoveAll<IHttpResponder>()
    .TryAddSingleton<IHttpResponder, MetadataResponder>();

if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddConsole();
}

var app = builder.Build();
var configuration = new OcelotPipelineConfiguration
{
    PreErrorResponderMiddleware = MyMiddlewares.PreErrorResponderMiddleware,
    ResponderMiddleware = MyMiddlewares.ResponderMiddleware, // can be switched off/on
};
await app.UseOcelot(configuration);
await app.RunAsync();
