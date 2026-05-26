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

// Serve static files (e.g., logo)
app.UseStaticFiles();

// Add middleware to handle "/" path with custom HTML response
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/" && context.Request.Method == "GET")
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "text/html; charset=utf-8";
        var htmlContent = await GetWelcomeHtmlAsync(context.RequestAborted);
        await context.Response.WriteAsync(htmlContent, context.RequestAborted);
    }
    else
    {
        await next();
    }
});

// IF Ocelot version is 24.1 and lower
// app.UseWebSockets();

var wsPipeline = new OcelotPipelineConfiguration
{
    WebSocketsMiddleware = MyWebSocketsProxyMiddleware,
};
await app.UseOcelot(wsPipeline); // IF Ocelot version is 25.0 and greater, internally called app.UseWebSockets() during building of Ocelot pipeline
await app.RunAsync();

static async Task<string> GetWelcomeHtmlAsync(CancellationToken cancellation)
{
    var htmlFilePath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "welcome.html");
    return await File.ReadAllTextAsync(htmlFilePath, cancellation);
}
static Task MyWebSocketsProxyMiddleware(HttpContext context, Func<Task> next)
{
    Task Next(HttpContext ctx) => next();
    var loggerFactory = context.RequestServices.GetRequiredService<IOcelotLoggerFactory>();
    var factory = context.RequestServices.GetRequiredService<IWebSocketsFactory>();
    var middleware = new CustomWebSocketsProxyMiddleware(Next, loggerFactory, factory);
    return middleware.Invoke(context);
}
