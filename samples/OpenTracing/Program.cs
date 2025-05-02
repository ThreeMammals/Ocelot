using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Samples.Web;
using Ocelot.Tracing.OpenTracing;
using OpenTracing.Util;

//_ = OcelotHostBuilder.Create(args);
var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddOcelot();
builder.Services
    .AddSingleton(serviceProvider =>
    {
        var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
        var config = new Jaeger.Configuration(builder.Environment.ApplicationName, loggerFactory);
        var tracer = config.GetTracer();
        GlobalTracer.Register(tracer);
        return tracer;
    })
    .AddOcelot(builder.Configuration)
    .AddOpenTracing();

if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddConsole();
}

var app = builder.Build();
await app.UseOcelot();
await app.RunAsync();
