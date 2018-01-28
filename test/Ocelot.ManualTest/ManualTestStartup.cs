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

namespace Ocelot.ManualTest
{
    public class ManualTestStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            Action<ConfigurationBuilderCachePart> settings = (x) =>
            {
                x.WithDictionaryHandle();
            };

            services.AddAuthentication()
                .AddJwtBearer("TestKey", x =>
                {
                    x.Authority = "test";
                    x.Audience = "test";
                });

            services.AddOcelot()
                    .AddCacheManager(settings)
                    .AddOpenTracing(option =>
                    {
                        option.CollectorUrl = "http://localhost:9618";
                        option.Service = "Ocelot.ManualTest";
                    })
                    .AddAdministration("/administration", "secret");
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseOcelot().Wait();
        }
    }
}
