using Ocelot.DependencyInjection;
using Ocelot.ManualTest.DelegatingHandlers;
using Ocelot.ManualTest.Middlewares;
using Ocelot.Middleware;
using Ocelot.Requester;
using System;
using System.Reflection;

namespace Ocelot.ManualTest.Actions;

public class Basic
{
    internal static async Task RunAsync(string[] args)
    {
        Console.WriteLine("Starting Ocelot... ");
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration
            .SetBasePath(builder.Environment.ContentRootPath)
            .AddOcelot();

        builder.Services
            /*.AddAuthentication()
            .AddJwtBearer("TestKey", x =>
            {
                x.Authority = "test";
                x.Audience = "test";
            });*/
            //.AddSingleton<QosDelegatingHandlerDelegate>((x, t, f) => new FakeHandler(f))
            .AddOcelot(builder.Configuration);
            //.AddDelegatingHandler<FakeHandler>(true);
            //.AddAdministration("/administration", "secret");

        builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
        if (builder.Environment.IsDevelopment())
        {
            builder.Logging.AddConsole();
        }

        var app = builder.Build();

        await app.UseOcelot();
        //await app.UseOcelot(pipeline =>
        //{
        //    pipeline.PreAuthenticationMiddleware = MetadataMiddleware.Invoke;
        //});
        await app.RunAsync(); // Ocelot will exit from this method when pressing Ctrl+C
    }
}
