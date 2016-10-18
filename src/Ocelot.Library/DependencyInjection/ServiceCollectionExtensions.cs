namespace Ocelot.Library.DependencyInjection
{
    using Authentication;
    using Configuration;
    using Configuration.Yaml;
    using DownstreamRouteFinder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Repository;
    using RequestBuilder;
    using Requester;
    using Responder;
    using UrlMatcher;
    using UrlTemplateReplacer;

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOcelot(this IServiceCollection services, IConfigurationRoot configurationRoot)
        {
            services.Configure<YamlConfiguration>(configurationRoot);

            // Add framework services.
            services.AddSingleton<IAddHeadersToRequest, AddHeadersToRequest>();
            services.AddSingleton<IClaimsParser, ClaimsParser>();
            services.AddSingleton<IConfigurationHeaderExtrator, ConfigurationHeaderExtrator>();
            services.AddSingleton<IConfigurationValidator, ConfigurationValidator>();
            services.AddSingleton<IOcelotConfiguration, OcelotConfiguration>();
            services.AddSingleton<IUrlPathToUrlTemplateMatcher, RegExUrlMatcher>();
            services.AddSingleton<ITemplateVariableNameAndValueFinder, TemplateVariableNameAndValueFinder>();
            services.AddSingleton<IDownstreamUrlTemplateVariableReplacer, DownstreamUrlTemplateVariableReplacer>();
            services.AddSingleton<IDownstreamRouteFinder, DownstreamRouteFinder>();
            services.AddSingleton<IHttpRequester, HttpClientHttpRequester>();
            services.AddSingleton<IHttpResponder, HttpContextResponder>();
            services.AddSingleton<IRequestBuilder, HttpRequestBuilder>();
            services.AddSingleton<IErrorsToHttpStatusCodeMapper, ErrorsToHttpStatusCodeMapper>();
            services.AddSingleton<IAuthenticationHandlerFactory, AuthenticationHandlerFactory>();
            services.AddSingleton<IAuthenticationHandlerCreator, AuthenticationHandlerCreator>();

            // see this for why we register this as singleton http://stackoverflow.com/questions/37371264/invalidoperationexception-unable-to-resolve-service-for-type-microsoft-aspnetc
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<IScopedRequestDataRepository, ScopedRequestDataRepository>();

            return services;
        }
    }
}
