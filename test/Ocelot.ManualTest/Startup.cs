using System;
using System.Threading.Tasks;
using CacheManager.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ocelot.Configuration.Provider;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using ConfigurationBuilder = Microsoft.Extensions.Configuration.ConfigurationBuilder;

namespace Ocelot.ManualTest
{
    public class Startup
    {
        private IdentityServerConfiguration _identityServerConfig;

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddJsonFile("configuration.json")
                .AddEnvironmentVariables();

            Configuration = builder.Build();

            var identityServerConfigProvider = new HardCodedIdentityServerConfigurationProvider();
            _identityServerConfig = identityServerConfigProvider.Get();
        }

        public IConfigurationRoot Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            Action<ConfigurationBuilderCachePart> settings = (x) =>
            {
                x.WithMicrosoftLogging(log =>
                {
                    log.AddConsole(LogLevel.Debug);
                })
                .WithDictionaryHandle();
            };

            services.AddOcelotOutputCaching(settings);
            services.AddOcelotFileConfiguration(Configuration);
            services.AddOcelot(_identityServerConfig);
        }

        public async void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));

            await app.UseOcelot(_identityServerConfig);
        }
    }
}
