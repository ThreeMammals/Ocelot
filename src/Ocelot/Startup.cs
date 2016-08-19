using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ocelot.Library.Middleware;

namespace Ocelot
{
    using Library.Infrastructure.HostUrlRepository;
    using Library.Infrastructure.UrlPathMatcher;
    using Library.Infrastructure.UrlPathReplacer;
    using Library.Infrastructure.UrlPathTemplateRepository;

    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddSingleton<IHostUrlMapRepository, InMemoryHostUrlMapRepository>();
            services.AddSingleton<IUrlPathToUrlPathTemplateMatcher, UrlPathToUrlPathTemplateMatcher>();
            services.AddSingleton<IHostUrlMapRepository, InMemoryHostUrlMapRepository>();
            services.AddSingleton<IUpstreamUrlPathTemplateVariableReplacer, UpstreamUrlPathTemplateVariableReplacer>();
            services.AddSingleton<IUrlPathTemplateMapRepository, InMemoryUrlPathTemplateMapRepository>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));

            loggerFactory.AddDebug();

            app.UseProxy();
        }
    }
}
