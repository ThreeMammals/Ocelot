using CacheManager.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ocelot.DependencyInjection;
using Ocelot.AcceptanceTests.Caching;

namespace Ocelot.AcceptanceTests
{
    public class StartupWithCustomCacheHandle : AcceptanceTestsStartup
    {
        public StartupWithCustomCacheHandle(IHostingEnvironment env) : base(env) { }

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
}
