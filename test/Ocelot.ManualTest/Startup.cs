using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CacheManager.Core;
using IdentityServer4.AccessTokenValidation;
using IdentityServer4.Models;
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
        private IIdentityServerConfiguration _identityServerConfig;

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

            var username = Environment.GetEnvironmentVariable("OCELOT_USERNAME");
            var hash = Environment.GetEnvironmentVariable("OCELOT_HASH");
            var salt = Environment.GetEnvironmentVariable("OCELOT_SALT");

            _identityServerConfig = new IdentityServerConfiguration(
                "admin",
                false,
                SupportedTokens.Both,
                "secret",
                new List<string> {"admin", "openid", "offline_access"},
                "Ocelot Administration",
                true,
                GrantTypes.ResourceOwnerPassword,
                AccessTokenType.Jwt,
                false,
                new List<User> 
                {
                    new User("admin", username, hash, salt)
                }
            );

            services.AddOcelot(Configuration, _identityServerConfig);
        }

        public async void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));

            await app.UseOcelot();
        }
    }
}
