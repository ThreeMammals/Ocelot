using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Authentication.Handler.Creator;
using Ocelot.Authentication.Handler.Factory;
using Ocelot.Authorisation;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.Parser;
using Ocelot.Configuration.Provider;
using Ocelot.Configuration.Repository;
using Ocelot.Configuration.Validator;
using Ocelot.Configuration.Yaml;
using Ocelot.DownstreamRouteFinder.Finder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.DownstreamUrlCreator.UrlTemplateReplacer;
using Ocelot.HeaderBuilder;
using Ocelot.HeaderBuilder.Parser;
using Ocelot.RequestBuilder.Builder;
using Ocelot.Requester;
using Ocelot.Responder;
using Ocelot.ScopedData;

namespace Ocelot.DependencyInjection
{
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
            services.AddSingleton<IAuthoriser, ClaimsAuthoriser>();
            services.AddSingleton<IAddHeadersToRequest, AddHeadersToRequest>();
            services.AddSingleton<IClaimsParser, ClaimsParser>();
            services.AddSingleton<IUrlPathToUrlTemplateMatcher, RegExUrlMatcher>();
            services.AddSingleton<ITemplateVariableNameAndValueFinder, TemplateVariableNameAndValueFinder>();
            services.AddSingleton<IDownstreamUrlTemplateVariableReplacer, DownstreamUrlTemplateVariableReplacer>();
            services.AddSingleton<IDownstreamRouteFinder, DownstreamRouteFinder.Finder.DownstreamRouteFinder>();
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
