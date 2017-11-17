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
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddStoreOcelotConfigurationInConsul(this IServiceCollection services, IConfigurationRoot configurationRoot)
        {
            var serviceDiscoveryPort = configurationRoot.GetValue("GlobalConfiguration:ServiceDiscoveryProvider:Port", 0);
            var serviceDiscoveryHost = configurationRoot.GetValue("GlobalConfiguration:ServiceDiscoveryProvider:Host", string.Empty);

            var config = new ServiceProviderConfigurationBuilder()
                .WithServiceDiscoveryProviderPort(serviceDiscoveryPort)
                .WithServiceDiscoveryProviderHost(serviceDiscoveryHost)
                .Build();

            services.AddSingleton<ServiceProviderConfiguration>(config);
            services.AddSingleton<ConsulFileConfigurationPoller>();
            services.AddSingleton<IFileConfigurationRepository, ConsulFileConfigurationRepository>();
            return services;
        }

        public static IServiceCollection AddOcelot(this IServiceCollection services,
            IConfigurationRoot configurationRoot)
        {
            Action<ConfigurationBuilderCachePart> defaultCachingSettings = x =>
            {
                x.WithDictionaryHandle();
            };

            return services.AddOcelot(configurationRoot, defaultCachingSettings);
        }

        public static IServiceCollection AddOcelot(this IServiceCollection services, IConfigurationRoot configurationRoot, Action<ConfigurationBuilderCachePart> settings)
        {
            var cacheManagerOutputCache = CacheFactory.Build<HttpResponseMessage>("OcelotOutputCache", settings);
            var ocelotOutputCacheManager = new OcelotCacheManagerCache<HttpResponseMessage>(cacheManagerOutputCache);
            services.TryAddSingleton<ICacheManager<HttpResponseMessage>>(cacheManagerOutputCache);
            services.TryAddSingleton<IOcelotCache<HttpResponseMessage>>(ocelotOutputCacheManager);

            var ocelotConfigCacheManagerOutputCache = CacheFactory.Build<IOcelotConfiguration>("OcelotConfigurationCache", settings);
            var ocelotConfigCacheManager = new OcelotCacheManagerCache<IOcelotConfiguration>(ocelotConfigCacheManagerOutputCache);
            services.TryAddSingleton<ICacheManager<IOcelotConfiguration>>(ocelotConfigCacheManagerOutputCache);
            services.TryAddSingleton<IOcelotCache<IOcelotConfiguration>>(ocelotConfigCacheManager);

              var fileConfigCacheManagerOutputCache = CacheFactory.Build<FileConfiguration>("FileConfigurationCache", settings);
            var fileConfigCacheManager = new OcelotCacheManagerCache<FileConfiguration>(fileConfigCacheManagerOutputCache);
            services.TryAddSingleton<ICacheManager<FileConfiguration>>(fileConfigCacheManagerOutputCache);
            services.TryAddSingleton<IOcelotCache<FileConfiguration>>(fileConfigCacheManager);

            services.Configure<FileConfiguration>(configurationRoot);
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

            var assembly = typeof(FileConfigurationController).GetTypeInfo().Assembly;

            services.AddMvcCore()
                .AddApplicationPart(assembly)
                .AddControllersAsServices()
                .AddAuthorization()
                .AddJsonFormatters();

            services.AddLogging();
            services.TryAddSingleton<IRegionCreator, RegionCreator>();
            services.TryAddSingleton<IFileConfigurationRepository, FileConfigurationRepository>();
            services.TryAddSingleton<IFileConfigurationSetter, FileConfigurationSetter>();
            services.TryAddSingleton<IFileConfigurationProvider, FileConfigurationProvider>();
            services.TryAddSingleton<IQosProviderHouse, QosProviderHouse>();
            services.TryAddSingleton<IQoSProviderFactory, QoSProviderFactory>();
            services.TryAddSingleton<IServiceDiscoveryProviderFactory, ServiceDiscoveryProviderFactory>();
            services.TryAddSingleton<ILoadBalancerFactory, LoadBalancerFactory>();
            services.TryAddSingleton<ILoadBalancerHouse, LoadBalancerHouse>();
            services.TryAddSingleton<IOcelotLoggerFactory, AspDotNetLoggerFactory>();
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
            services.TryAddSingleton<IRateLimitCounterHandler, MemoryCacheRateLimitCounterHandler>();
            services.TryAddSingleton<IHttpClientCache, MemoryHttpClientCache>();
            services.TryAddSingleton<IRequestMapper, RequestMapper>();
            services.TryAddSingleton<IHttpHandlerOptionsCreator, HttpHandlerOptionsCreator>();

            // see this for why we register this as singleton http://stackoverflow.com/questions/37371264/invalidoperationexception-unable-to-resolve-service-for-type-microsoft-aspnetc
            // could maybe use a scoped data repository
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.TryAddScoped<IRequestScopedDataRepository, HttpDataRepository>();
            services.AddMemoryCache();

            //Used to log the the start and ending of middleware
            services.TryAddSingleton<OcelotDiagnosticListener>();
            services.AddMiddlewareAnalysis();
            services.AddWebEncoders();

            var identityServerConfiguration = IdentityServerConfigurationCreator.GetIdentityServerConfiguration();

            if (identityServerConfiguration != null)
            {
                services.AddIdentityServer(identityServerConfiguration, configurationRoot);
            }

            return services;
        }

        private static void AddIdentityServer(this IServiceCollection services, IIdentityServerConfiguration identityServerConfiguration, IConfigurationRoot configurationRoot) 
        {
            services.TryAddSingleton<IIdentityServerConfiguration>(identityServerConfiguration);
            services.TryAddSingleton<IHashMatcher, HashMatcher>();
            var identityServerBuilder = services
                .AddIdentityServer(o => {
                    o.IssuerUri = "Ocelot";
                })
                .AddInMemoryApiResources(Resources(identityServerConfiguration))
                .AddInMemoryClients(Client(identityServerConfiguration))
                .AddResourceOwnerValidator<OcelotResourceOwnerPasswordValidator>();

            //todo - refactor a method so we know why this is happening
            var whb = services.First(x => x.ServiceType == typeof(IWebHostBuilder));
            var urlFinder = new BaseUrlFinder((IWebHostBuilder)whb.ImplementationInstance);
            var baseSchemeUrlAndPort = urlFinder.Find();
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
                .AddIdentityServerAuthentication(o =>
                {
                    var adminPath = configurationRoot.GetValue("GlobalConfiguration:AdministrationPath", string.Empty);
                    o.Authority = baseSchemeUrlAndPort + adminPath;
                    o.ApiName = identityServerConfiguration.ApiName;
                    o.RequireHttpsMetadata = identityServerConfiguration.RequireHttps;
                    o.SupportedTokens = SupportedTokens.Both;
                    o.ApiSecret = identityServerConfiguration.ApiSecret;
                });

                //todo - refactor naming..
                if (string.IsNullOrEmpty(identityServerConfiguration.CredentialsSigningCertificateLocation) || string.IsNullOrEmpty(identityServerConfiguration.CredentialsSigningCertificatePassword))
                {
                    identityServerBuilder.AddDeveloperSigningCredential();
                }
                else
                {
                    //todo - refactor so calls method?
                    var cert = new X509Certificate2(identityServerConfiguration.CredentialsSigningCertificateLocation, identityServerConfiguration.CredentialsSigningCertificatePassword);
                    identityServerBuilder.AddSigningCredential(cert);
                }
        }

        private static List<ApiResource> Resources(IIdentityServerConfiguration identityServerConfiguration)
        {
            return new List<ApiResource>
            {
                new ApiResource(identityServerConfiguration.ApiName, identityServerConfiguration.ApiName)
                {
                    ApiSecrets = new List<Secret>
                    {
                        new Secret
                        {
                            Value = identityServerConfiguration.ApiSecret.Sha256()
                        }
                    }
                }
            };
        }

        private static List<Client> Client(IIdentityServerConfiguration identityServerConfiguration) 
        {
            return new List<Client>
            {
                new Client
                {
                    ClientId = identityServerConfiguration.ApiName,
                    AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                    ClientSecrets = new List<Secret> {new Secret(identityServerConfiguration.ApiSecret.Sha256())},
                    AllowedScopes = { identityServerConfiguration.ApiName }
                }
            };
        }
    }
}
