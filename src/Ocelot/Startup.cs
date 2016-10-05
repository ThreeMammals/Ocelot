using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ocelot.Library.Infrastructure.DownstreamRouteFinder;
using Ocelot.Library.Infrastructure.Repository;
using Ocelot.Library.Infrastructure.Requester;
using Ocelot.Library.Infrastructure.Responder;
using Ocelot.Library.Middleware;

namespace Ocelot
{
    using Library.Infrastructure.Configuration;
    using Library.Infrastructure.UrlMatcher;
    using Library.Infrastructure.UrlTemplateReplacer;

    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddYamlFile("configuration.yaml")
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();

            services.Configure<Configuration>(Configuration);

            // Add framework services.
            services.AddSingleton<IUrlPathToUrlTemplateMatcher, UrlPathToUrlTemplateMatcher>();
            services.AddSingleton<IDownstreamUrlTemplateVariableReplacer, DownstreamUrlTemplateVariableReplacer>();
            services.AddSingleton<IDownstreamRouteFinder, DownstreamRouteFinder>();
            services.AddSingleton<IHttpRequester, HttpClientHttpRequester>();
            services.AddSingleton<IHttpResponder, HttpContextResponder>();

            // see this for why we register this as singleton http://stackoverflow.com/questions/37371264/invalidoperationexception-unable-to-resolve-service-for-type-microsoft-aspnetc
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<IScopedRequestDataRepository, ScopedRequestDataRepository>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));

            loggerFactory.AddDebug();

            app.UseDownstreamRouteFinderMiddleware();

            app.UserDownstreamUrlCreatorMiddleware();

            app.UseHttpRequesterMiddleware();
        }
    }
}
