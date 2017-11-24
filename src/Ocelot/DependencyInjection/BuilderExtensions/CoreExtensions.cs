using CacheManager.Core;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ocelot.Authorisation;
using Ocelot.Cache;
using Ocelot.Claims;
using Ocelot.Configuration.Authentication;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Parser;
using Ocelot.Configuration.Provider;
using Ocelot.Configuration.Repository;
using Ocelot.Configuration.Setter;
using Ocelot.Configuration.Validator;
using Ocelot.Controllers;
using Ocelot.DownstreamRouteFinder.Finder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.DownstreamUrlCreator;
using Ocelot.DownstreamUrlCreator.UrlTemplateReplacer;
using Ocelot.Headers;
using Ocelot.Infrastructure.Claims.Parser;
using Ocelot.Infrastructure.RequestData;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.QueryStrings;
using Ocelot.RateLimit;
using Ocelot.Request.Builder;
using Ocelot.Request.Mapper;
using Ocelot.Requester;
using Ocelot.Requester.QoS;
using Ocelot.Responder;
using Ocelot.ServiceDiscovery;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using FileConfigurationProvider = Ocelot.Configuration.Provider.FileConfigurationProvider;

namespace Ocelot.DependencyInjection
{
    public static class OcelotBuilderExtensionsCore
    {
        public static IOcelotBuilder AddRequiredBaseServices(this IOcelotBuilder builder) {

            var services = builder.Services;

            services.Configure<FileConfiguration>(builder.Configuration);
            services.TryAddSingleton<IOcelotConfigurationCreator, FileOcelotConfigurationCreator>();
            services.TryAddSingleton<IOcelotConfigurationRepository, InMemoryOcelotConfigurationRepository>();
            services.TryAddSingleton<IConfigurationValidator, FileConfigurationValidator>();
            services.TryAddSingleton<IClaimsToThingCreator, ClaimsToThingCreator>();
            services.TryAddSingleton<IAuthenticationOptionsCreator, AuthenticationOptionsCreator>();
            services.TryAddSingleton<IUpstreamTemplatePatternCreator, UpstreamTemplatePatternCreator>();
            services.TryAddSingleton<IRequestIdKeyCreator, RequestIdKeyCreator>();
            services.TryAddSingleton<IServiceProviderConfigurationCreator,ServiceProviderConfigurationCreator>();
            services.TryAddSingleton<IQoSOptionsCreator, QoSOptionsCreator>();
            services.TryAddSingleton<IReRouteOptionsCreator, ReRouteOptionsCreator>();
            services.TryAddSingleton<IRateLimitOptionsCreator, RateLimitOptionsCreator>();
            services.TryAddSingleton<IBaseUrlFinder, BaseUrlFinder>();
            services.TryAddSingleton<IRegionCreator, RegionCreator>();
            services.TryAddSingleton<IFileConfigurationRepository, FileConfigurationRepository>();
            services.TryAddSingleton<IFileConfigurationSetter, FileConfigurationSetter>();
            services.TryAddSingleton<IFileConfigurationProvider, FileConfigurationProvider>();

            services.TryAddSingleton<IUrlBuilder, UrlBuilder>();
            services.TryAddSingleton<IRemoveOutputHeaders, RemoveOutputHeaders>();
            services.TryAddSingleton<IOcelotConfigurationProvider, OcelotConfigurationProvider>();
            services.TryAddSingleton<IClaimToThingConfigurationParser, ClaimToThingConfigurationParser>();
            services.TryAddSingleton<IClaimsAuthoriser, ClaimsAuthoriser>();
            services.TryAddSingleton<IScopesAuthoriser, ScopesAuthoriser>();
            services.TryAddSingleton<IAddClaimsToRequest, AddClaimsToRequest>();
            services.TryAddSingleton<IAddHeadersToRequest, AddHeadersToRequest>();
            services.TryAddSingleton<IAddQueriesToRequest, AddQueriesToRequest>();
            services.TryAddSingleton<IClaimsParser, ClaimsParser>();
            services.TryAddSingleton<IUrlPathToUrlTemplateMatcher, RegExUrlMatcher>();
            services.TryAddSingleton<IUrlPathPlaceholderNameAndValueFinder, UrlPathPlaceholderNameAndValueFinder>();
            services.TryAddSingleton<IDownstreamPathPlaceholderReplacer, DownstreamTemplatePathPlaceholderReplacer>();
            services.TryAddSingleton<IDownstreamRouteFinder, DownstreamRouteFinder.Finder.DownstreamRouteFinder>();
            services.TryAddSingleton<IHttpRequester, HttpClientHttpRequester>();
            services.TryAddSingleton<IHttpResponder, HttpContextResponder>();
            services.TryAddSingleton<IRequestCreator, HttpRequestCreator>();
            services.TryAddSingleton<IErrorsToHttpStatusCodeMapper, ErrorsToHttpStatusCodeMapper>();

            services.TryAddSingleton<IHttpClientCache, MemoryHttpClientCache>();
            services.TryAddSingleton<IRequestMapper, RequestMapper>();
            services.TryAddSingleton<IHttpHandlerOptionsCreator, HttpHandlerOptionsCreator>();
            // see this for why we register this as singleton http://stackoverflow.com/questions/37371264/invalidoperationexception-unable-to-resolve-service-for-type-microsoft-aspnetc
            // could maybe use a scoped data repository
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.TryAddScoped<IRequestScopedDataRepository, HttpDataRepository>();
            services.AddMemoryCache();
            services.TryAddSingleton<OcelotDiagnosticListener>();

            return builder;
        }

        public static IOcelotBuilder AddAspNetCoreServices(this IOcelotBuilder builder) {
            var assembly = typeof(FileConfigurationController).GetTypeInfo().Assembly;

            var services = builder.Services;

            services.AddMvcCore()
                .AddApplicationPart(assembly)
                .AddControllersAsServices()
                .AddAuthorization()
                .AddJsonFormatters();

            services.AddLogging();
            services.AddMiddlewareAnalysis();
            services.AddWebEncoders();

            return builder;
        }

        public static IOcelotBuilder AddCacheManager(this IOcelotBuilder builder, Action<ConfigurationBuilderCachePart> settings)
        {
            var cacheManagerOutputCache = CacheFactory.Build<HttpResponseMessage>("OcelotOutputCache", settings);
            var ocelotOutputCacheManager = new OcelotCacheManagerCache<HttpResponseMessage>(cacheManagerOutputCache);

            var services = builder.Services;

            services.RemoveAll(typeof(ICacheManager<HttpResponseMessage>));
            services.RemoveAll(typeof(IOcelotCache<HttpResponseMessage>));
            services.AddSingleton<ICacheManager<HttpResponseMessage>>(cacheManagerOutputCache);
            services.AddSingleton<IOcelotCache<HttpResponseMessage>>(ocelotOutputCacheManager);

            var ocelotConfigCacheManagerOutputCache = CacheFactory.Build<IOcelotConfiguration>("OcelotConfigurationCache", settings);
            var ocelotConfigCacheManager = new OcelotCacheManagerCache<IOcelotConfiguration>(ocelotConfigCacheManagerOutputCache);
            services.RemoveAll(typeof(ICacheManager<IOcelotConfiguration>));
            services.RemoveAll(typeof(IOcelotCache<IOcelotConfiguration>));
            services.AddSingleton<ICacheManager<IOcelotConfiguration>>(ocelotConfigCacheManagerOutputCache);
            services.AddSingleton<IOcelotCache<IOcelotConfiguration>>(ocelotConfigCacheManager);

            var fileConfigCacheManagerOutputCache = CacheFactory.Build<FileConfiguration>("FileConfigurationCache", settings);
            var fileConfigCacheManager = new OcelotCacheManagerCache<FileConfiguration>(fileConfigCacheManagerOutputCache);
            services.RemoveAll(typeof(ICacheManager<FileConfiguration>));
            services.RemoveAll(typeof(IOcelotCache<FileConfiguration>));
            services.AddSingleton<ICacheManager<FileConfiguration>>(fileConfigCacheManagerOutputCache);
            services.AddSingleton<IOcelotCache<FileConfiguration>>(fileConfigCacheManager);

            return builder;
        }

        public static IOcelotBuilder AddLogging(this IOcelotBuilder builder)
        {
            return builder.AddLogging<AspDotNetLoggerFactory>();
        }

        public static IOcelotBuilder AddLogging<LoggerFactoryType>(this IOcelotBuilder builder)
            where LoggerFactoryType : class, IOcelotLoggerFactory
        {
            var services = builder.Services;

            services.TryAddSingleton<IOcelotLoggerFactory, LoggerFactoryType>();

            return builder;
        }

        public static IOcelotBuilder AddQos(this IOcelotBuilder builder)
        {
            return builder.AddQos<QosProviderHouse, QoSProviderFactory>();
        }

        public static IOcelotBuilder AddQos<QosProviderHouseType, QoSProviderFactoryType>(this IOcelotBuilder builder)
            where QosProviderHouseType : class, IQosProviderHouse
            where QoSProviderFactoryType : class, IQoSProviderFactory
        {
            var services = builder.Services;

            services.TryAddSingleton<IQosProviderHouse, QosProviderHouseType>();
            services.TryAddSingleton<IQoSProviderFactory, QoSProviderFactoryType>();

            return builder;
        }

        public static IOcelotBuilder AddServiceDiscovery(this IOcelotBuilder builder)
        {
            return builder.AddServiceDiscovery<ServiceDiscoveryProviderFactory>();
        }

        public static IOcelotBuilder AddServiceDiscovery<ServiceDiscoveryProviderFactoryType>(this IOcelotBuilder builder)
            where ServiceDiscoveryProviderFactoryType : class, IServiceDiscoveryProviderFactory
        {
            var services = builder.Services;

            services.TryAddSingleton<IServiceDiscoveryProviderFactory, ServiceDiscoveryProviderFactoryType>();

            return builder;
        }

        public static IOcelotBuilder AddLoadBalancer(this IOcelotBuilder builder)
        {
            return builder.AddLoadBalancer<LoadBalancerFactory, LoadBalancerHouse>();
        }

        public static IOcelotBuilder AddLoadBalancer<LoadBalancerFactoryType, LoadBalancerHouseType>(this IOcelotBuilder builder)
            where LoadBalancerFactoryType : class, ILoadBalancerFactory
            where LoadBalancerHouseType : class, ILoadBalancerHouse
        {
            var services = builder.Services;

            services.TryAddSingleton<ILoadBalancerFactory, LoadBalancerFactoryType>();
            services.TryAddSingleton<ILoadBalancerHouse, LoadBalancerHouseType>();

            return builder;
        }

        public static IOcelotBuilder AddRateLimitCounter(this IOcelotBuilder builder)
        {
            return builder.AddRateLimitCounter<MemoryCacheRateLimitCounterHandler>();
        }

        public static IOcelotBuilder AddRateLimitCounter<RateLimitCounterType>(this IOcelotBuilder builder)
            where RateLimitCounterType : class, IRateLimitCounterHandler
        {
            var services = builder.Services;

            services.TryAddSingleton<IRateLimitCounterHandler, RateLimitCounterType>();

            return builder;
        }
    }
}
