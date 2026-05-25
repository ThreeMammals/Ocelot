using Ocelot.DependencyInjection;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Samples.WebSocket;
using Ocelot.WebSockets;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddOcelot();
builder.Services
    .AddOcelot(builder.Configuration);

if (builder.Environment.IsDevelopment())
    builder.Logging.AddConsole();

var app = builder.Build();
app.UseWebSockets();

var wsPipeline = new OcelotPipelineConfiguration
{
    WebSocketsProxyMiddleware = (context, next) =>
    {
        Task Next(HttpContext ctx) => next();
        var loggerFactory = context.RequestServices.GetRequiredService<IOcelotLoggerFactory>();
        var factory = context.RequestServices.GetRequiredService<IWebSocketsFactory>();
        var middleware = new CustomWebSocketsProxyMiddleware(loggerFactory, Next, factory);
        return middleware.Invoke(context);
    },
};
await app.UseOcelot(wsPipeline);
await app.RunAsync();
