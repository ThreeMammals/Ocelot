namespace Ocelot.DependencyInjection
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Ocelot.Authorisation;
    using Ocelot.Cache;
    using Ocelot.Claims;
    using Ocelot.Configuration.Creator;
    using Ocelot.Configuration.File;
    using Ocelot.Configuration.Parser;
    using Ocelot.Configuration.Repository;
    using Ocelot.Configuration.Setter;
    using Ocelot.Configuration.Validator;
    using Ocelot.DownstreamRouteFinder.Finder;
    using Ocelot.DownstreamRouteFinder.UrlMatcher;
    using Ocelot.DownstreamUrlCreator.UrlTemplateReplacer;
    using Ocelot.Headers;
    using Ocelot.Infrastructure.Claims.Parser;
    using Ocelot.Infrastructure.RequestData;
    using Ocelot.LoadBalancer.LoadBalancers;
    using Ocelot.Logging;
    using Ocelot.Middleware;
    using Ocelot.QueryStrings;
    using Ocelot.RateLimit;
    using Ocelot.Request.Mapper;
    using Ocelot.Requester;
    using Ocelot.Requester.QoS;
    using Ocelot.Responder;
    using Ocelot.ServiceDiscovery;
    using System.Reflection;
    using Ocelot.Configuration;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using System.Net.Http;
    using Ocelot.Infrastructure;
    using Ocelot.Middleware.Multiplexer;
    using Ocelot.Request.Creator;

    public class OcelotBuilder : IOcelotBuilder
    {
        public IServiceCollection Services { get; }
        public IConfiguration Configuration { get; }

        public OcelotBuilder(IServiceCollection services, IConfiguration configurationRoot)
        {
            Configuration = configurationRoot;
            Services = services;
            Services.Configure<FileConfiguration>(configurationRoot);

            Services.TryAddSingleton<IOcelotCache<FileConfiguration>, InMemoryCache<FileConfiguration>>();
            Services.TryAddSingleton<IOcelotCache<CachedResponse>, InMemoryCache<CachedResponse>>();
            Services.TryAddSingleton<IHttpResponseHeaderReplacer, HttpResponseHeaderReplacer>();
            Services.TryAddSingleton<IHttpContextRequestHeaderReplacer, HttpContextRequestHeaderReplacer>();
            Services.TryAddSingleton<IHeaderFindAndReplaceCreator, HeaderFindAndReplaceCreator>();
            Services.TryAddSingleton<IInternalConfigurationCreator, FileInternalConfigurationCreator>();
            Services.TryAddSingleton<IInternalConfigurationRepository, InMemoryInternalConfigurationRepository>();
            Services.TryAddSingleton<IConfigurationValidator, FileConfigurationFluentValidator>();
            Services.TryAddSingleton<IClaimsToThingCreator, ClaimsToThingCreator>();
            Services.TryAddSingleton<IAuthenticationOptionsCreator, AuthenticationOptionsCreator>();
            Services.TryAddSingleton<IUpstreamTemplatePatternCreator, UpstreamTemplatePatternCreator>();
            Services.TryAddSingleton<IRequestIdKeyCreator, RequestIdKeyCreator>();
            Services.TryAddSingleton<IServiceProviderConfigurationCreator,ServiceProviderConfigurationCreator>();
            Services.TryAddSingleton<IQoSOptionsCreator, QoSOptionsCreator>();
            Services.TryAddSingleton<IReRouteOptionsCreator, ReRouteOptionsCreator>();
            Services.TryAddSingleton<IRateLimitOptionsCreator, RateLimitOptionsCreator>();
            Services.TryAddSingleton<IBaseUrlFinder, BaseUrlFinder>();
            Services.TryAddSingleton<IRegionCreator, RegionCreator>();
            Services.TryAddSingleton<IFileConfigurationRepository, DiskFileConfigurationRepository>();
            Services.TryAddSingleton<IFileConfigurationSetter, FileAndInternalConfigurationSetter>();
            Services.TryAddSingleton<IServiceDiscoveryProviderFactory, ServiceDiscoveryProviderFactory>();
            Services.TryAddSingleton<ILoadBalancerFactory, LoadBalancerFactory>();
            Services.TryAddSingleton<ILoadBalancerHouse, LoadBalancerHouse>();
            Services.TryAddSingleton<IOcelotLoggerFactory, AspDotNetLoggerFactory>();
            Services.TryAddSingleton<IRemoveOutputHeaders, RemoveOutputHeaders>();
            Services.TryAddSingleton<IClaimToThingConfigurationParser, ClaimToThingConfigurationParser>();
            Services.TryAddSingleton<IClaimsAuthoriser, ClaimsAuthoriser>();
            Services.TryAddSingleton<IScopesAuthoriser, ScopesAuthoriser>();
            Services.TryAddSingleton<IAddClaimsToRequest, AddClaimsToRequest>();
            Services.TryAddSingleton<IAddHeadersToRequest, AddHeadersToRequest>();
            Services.TryAddSingleton<IAddQueriesToRequest, AddQueriesToRequest>();
            Services.TryAddSingleton<IClaimsParser, ClaimsParser>();
            Services.TryAddSingleton<IUrlPathToUrlTemplateMatcher, RegExUrlMatcher>();
            Services.TryAddSingleton<IPlaceholderNameAndValueFinder, UrlPathPlaceholderNameAndValueFinder>();
            Services.TryAddSingleton<IDownstreamPathPlaceholderReplacer, DownstreamTemplatePathPlaceholderReplacer>();
            Services.AddSingleton<IDownstreamRouteProvider, DownstreamRouteFinder>();
            Services.AddSingleton<IDownstreamRouteProvider, DownstreamRouteCreator>();
            Services.TryAddSingleton<IDownstreamRouteProviderFactory, DownstreamRouteProviderFactory>();
            Services.TryAddSingleton<IHttpRequester, HttpClientHttpRequester>();
            Services.TryAddSingleton<IHttpResponder, HttpContextResponder>();
            Services.TryAddSingleton<IErrorsToHttpStatusCodeMapper, ErrorsToHttpStatusCodeMapper>();
            Services.TryAddSingleton<IRateLimitCounterHandler, MemoryCacheRateLimitCounterHandler>();
            Services.TryAddSingleton<IHttpClientCache, MemoryHttpClientCache>();
            Services.TryAddSingleton<IRequestMapper, RequestMapper>();
            Services.TryAddSingleton<IHttpHandlerOptionsCreator, HttpHandlerOptionsCreator>();
            Services.TryAddSingleton<IDownstreamAddressesCreator, DownstreamAddressesCreator>();
            Services.TryAddSingleton<IDelegatingHandlerHandlerFactory, DelegatingHandlerHandlerFactory>();
            Services.TryAddSingleton<IHttpRequester, HttpClientHttpRequester>();

            // see this for why we register this as singleton http://stackoverflow.com/questions/37371264/invalidoperationexception-unable-to-resolve-service-for-type-microsoft-aspnetc
            // could maybe use a scoped data repository
            Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            Services.TryAddSingleton<IRequestScopedDataRepository, HttpDataRepository>();
            Services.AddMemoryCache();
            Services.TryAddSingleton<OcelotDiagnosticListener>();
            Services.TryAddSingleton<IMultiplexer, Multiplexer>();
            Services.TryAddSingleton<IResponseAggregator, SimpleJsonResponseAggregator>();
            Services.AddSingleton<ITracingHandlerFactory, TracingHandlerFactory>();
            Services.TryAddSingleton<IFileConfigurationPollerOptions, InMemoryFileConfigurationPollerOptions>();
            Services.TryAddSingleton<IAddHeadersToResponse, AddHeadersToResponse>();
            Services.TryAddSingleton<IPlaceholders, Placeholders>();
            Services.TryAddSingleton<IResponseAggregatorFactory, InMemoryResponseAggregatorFactory>();
            Services.TryAddSingleton<IDefinedAggregatorProvider, ServiceLocatorDefinedAggregatorProvider>();
            Services.TryAddSingleton<IDownstreamRequestCreator, DownstreamRequestCreator>();
            Services.TryAddSingleton<IFrameworkDescription, FrameworkDescription>();
            Services.TryAddSingleton<IQoSFactory, QoSFactory>();
            Services.TryAddSingleton<IExceptionToErrorMapper, HttpExeptionToErrorMapper>();

            //add asp.net services..
            var assembly = typeof(FileConfigurationController).GetTypeInfo().Assembly;

            Services.AddMvcCore()
                .AddApplicationPart(assembly)
                .AddControllersAsServices()
                .AddAuthorization()
                .AddJsonFormatters();

            Services.AddLogging();
            Services.AddMiddlewareAnalysis();
            Services.AddWebEncoders();
        }

        public IOcelotBuilder AddSingletonDefinedAggregator<T>() 
            where T : class, IDefinedAggregator
        {
            Services.AddSingleton<IDefinedAggregator, T>();
            return this;
        }

        public IOcelotBuilder AddTransientDefinedAggregator<T>() 
            where T : class, IDefinedAggregator
        {
            Services.AddTransient<IDefinedAggregator, T>();
            return this;
        }

        public IOcelotBuilder AddDelegatingHandler<THandler>(bool global = false) 
            where THandler : DelegatingHandler 
        {
            if(global)
            {
                Services.AddTransient<THandler>();
                Services.AddTransient<GlobalDelegatingHandler>(s => {
                    var service = s.GetService<THandler>();
                    return new GlobalDelegatingHandler(service);
                });
            }
            else
            {
                Services.AddTransient<DelegatingHandler, THandler>();
            }

            return this;
        }
    }
}
