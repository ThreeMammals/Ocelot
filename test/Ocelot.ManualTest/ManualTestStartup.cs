using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

namespace Ocelot.ManualTest
{
    public class ManualTestStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication()
                .AddJwtBearer("TestKey", x =>
                {
                    x.Authority = "test";
                    x.Audience = "test";
                });

            services.AddOcelot()
                    .AddCacheManager(x =>
                    {
                        x.WithDictionaryHandle();
                    })
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