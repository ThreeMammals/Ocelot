using System;
using CacheManager.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using ConfigurationBuilder = Microsoft.Extensions.Configuration.ConfigurationBuilder;
using Ocelot.AcceptanceTests.Caching;

namespace Ocelot.AcceptanceTests
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddJsonFile("configuration.json")
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        public virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddOcelot(Configuration);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));

            app.UseOcelot().Wait();
        }
    }

    public class Startup_WithCustomCacheHandle : Startup
    {
        public Startup_WithCustomCacheHandle(IHostingEnvironment env) : base(env) { }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddOcelot(Configuration)
                .AddCacheManager((x) =>
                {
                    x.WithMicrosoftLogging(log =>
                    {
                        log.AddConsole(LogLevel.Debug);
                    })
                    .WithJsonSerializer()
                    .WithHandle(typeof(InMemoryJsonHandle<>));
                });
        }
    }

    public class Startup_WithConsul_And_CustomCacheHandle : Startup
    {
        public Startup_WithConsul_And_CustomCacheHandle(IHostingEnvironment env) : base(env) { }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddOcelot(Configuration)
                .AddCacheManager((x) =>
                {
                    x.WithMicrosoftLogging(log =>
                    {
                        log.AddConsole(LogLevel.Debug);
                    })
                    .WithJsonSerializer()
                    .WithHandle(typeof(InMemoryJsonHandle<>));
                })
                .AddStoreOcelotConfigurationInConsul();
        }
    }
}
