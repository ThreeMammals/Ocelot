using Ocelot.Library.Configuration.Creator;
using Ocelot.Library.Configuration.Parser;
using Ocelot.Library.Configuration.Provider;
using Ocelot.Library.Configuration.Repository;

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
        public static IServiceCollection AddOcelotYamlConfiguration(this IServiceCollection services, IConfigurationRoot configurationRoot)
        {
            // initial configuration from yaml
            services.Configure<YamlConfiguration>(configurationRoot);

            // ocelot services.
            services.AddSingleton<IOcelotConfigurationCreator, YamlOcelotConfigurationCreator>();
            services.AddSingleton<IOcelotConfigurationProvider, YamlOcelotConfigurationProvider>();
            services.AddSingleton<IOcelotConfigurationRepository, InMemoryOcelotConfigurationRepository>();
            services.AddSingleton<IClaimToHeaderConfigurationParser, ClaimToHeaderConfigurationParser>();
            services.AddSingleton<IConfigurationValidator, ConfigurationValidator>();

            return services;
        }

        public static IServiceCollection AddOcelot(this IServiceCollection services)
        {
            // framework services
            services.AddMvcCore().AddJsonFormatters();
            services.AddLogging();

            // ocelot services.
            services.AddSingleton<IAddHeadersToRequest, AddHeadersToRequest>();
            services.AddSingleton<IClaimsParser, ClaimsParser>();
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
            // could maybe use a scoped data repository
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<IScopedRequestDataRepository, ScopedRequestDataRepository>();

            return services;
        }
    }
}
