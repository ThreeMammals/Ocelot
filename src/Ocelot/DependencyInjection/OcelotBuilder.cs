namespace Ocelot.DependencyInjection
{
    using CacheManager.Core;
    using IdentityServer4.Models;
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
    using System;
    using System.Collections.Generic;
    using System.IdentityModel.Tokens.Jwt;
    using System.Reflection;
    using System.Security.Cryptography.X509Certificates;
    using IdentityServer4.AccessTokenValidation;
    using Microsoft.AspNetCore.Builder;
    using Ocelot.Configuration;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using System.Net.Http;
    using Butterfly.Client.AspNetCore;
    using Ocelot.Infrastructure;
    using Ocelot.Infrastructure.Consul;
    using Butterfly.Client.Tracing;
    using Ocelot.Middleware.Multiplexer;
    using Pivotal.Discovery.Client;
    using ServiceDiscovery.Providers;

    public class OcelotBuilder : IOcelotBuilder
    {
        private readonly IServiceCollection _services;
        private readonly IConfiguration _configurationRoot;

        public OcelotBuilder(IServiceCollection services, IConfiguration configurationRoot)
        {
            _configurationRoot = configurationRoot;
            _services = services;    
           
            //add default cache settings...
            Action<ConfigurationBuilderCachePart> defaultCachingSettings = x =>
            {
                x.WithDictionaryHandle();
            };

            AddCacheManager(defaultCachingSettings);

            //add ocelot services...
            _services.Configure<FileConfiguration>(configurationRoot);
            _services.TryAddSingleton<IHttpResponseHeaderReplacer, HttpResponseHeaderReplacer>();
            _services.TryAddSingleton<IHttpContextRequestHeaderReplacer, HttpContextRequestHeaderReplacer>();
            _services.TryAddSingleton<IHeaderFindAndReplaceCreator, HeaderFindAndReplaceCreator>();
            _services.TryAddSingleton<IInternalConfigurationCreator, FileInternalConfigurationCreator>();
            _services.TryAddSingleton<IInternalConfigurationRepository, InMemoryInternalConfigurationRepository>();
            _services.TryAddSingleton<IConfigurationValidator, FileConfigurationFluentValidator>();
            _services.TryAddSingleton<IClaimsToThingCreator, ClaimsToThingCreator>();
            _services.TryAddSingleton<IAuthenticationOptionsCreator, AuthenticationOptionsCreator>();
            _services.TryAddSingleton<IUpstreamTemplatePatternCreator, UpstreamTemplatePatternCreator>();
            _services.TryAddSingleton<IRequestIdKeyCreator, RequestIdKeyCreator>();
            _services.TryAddSingleton<IServiceProviderConfigurationCreator,ServiceProviderConfigurationCreator>();
            _services.TryAddSingleton<IQoSOptionsCreator, QoSOptionsCreator>();
            _services.TryAddSingleton<IReRouteOptionsCreator, ReRouteOptionsCreator>();
            _services.TryAddSingleton<IRateLimitOptionsCreator, RateLimitOptionsCreator>();
            _services.TryAddSingleton<IBaseUrlFinder, BaseUrlFinder>();
            _services.TryAddSingleton<IRegionCreator, RegionCreator>();
            _services.TryAddSingleton<IFileConfigurationRepository, DiskFileConfigurationRepository>();
            _services.TryAddSingleton<IFileConfigurationSetter, FileAndInternalConfigurationSetter>();
            _services.TryAddSingleton<IQosProviderHouse, QosProviderHouse>();
            _services.TryAddSingleton<IQoSProviderFactory, QoSProviderFactory>();
            _services.TryAddSingleton<IServiceDiscoveryProviderFactory, ServiceDiscoveryProviderFactory>();
            _services.TryAddSingleton<ILoadBalancerFactory, LoadBalancerFactory>();
            _services.TryAddSingleton<ILoadBalancerHouse, LoadBalancerHouse>();
            _services.TryAddSingleton<IOcelotLoggerFactory, AspDotNetLoggerFactory>();
            _services.TryAddSingleton<IRemoveOutputHeaders, RemoveOutputHeaders>();
            _services.TryAddSingleton<IClaimToThingConfigurationParser, ClaimToThingConfigurationParser>();
            _services.TryAddSingleton<IClaimsAuthoriser, ClaimsAuthoriser>();
            _services.TryAddSingleton<IScopesAuthoriser, ScopesAuthoriser>();
            _services.TryAddSingleton<IAddClaimsToRequest, AddClaimsToRequest>();
            _services.TryAddSingleton<IAddHeadersToRequest, AddHeadersToRequest>();
            _services.TryAddSingleton<IAddQueriesToRequest, AddQueriesToRequest>();
            _services.TryAddSingleton<IClaimsParser, ClaimsParser>();
            _services.TryAddSingleton<IUrlPathToUrlTemplateMatcher, RegExUrlMatcher>();
            _services.TryAddSingleton<IPlaceholderNameAndValueFinder, UrlPathPlaceholderNameAndValueFinder>();
            _services.TryAddSingleton<IDownstreamPathPlaceholderReplacer, DownstreamTemplatePathPlaceholderReplacer>();
            _services.AddSingleton<IDownstreamRouteProvider, DownstreamRouteFinder>();
            _services.AddSingleton<IDownstreamRouteProvider, Ocelot.DownstreamRouteFinder.Finder.DownstreamRouteCreator>();
            _services.TryAddSingleton<IDownstreamRouteProviderFactory, Ocelot.DownstreamRouteFinder.Finder.DownstreamRouteProviderFactory>();
            _services.TryAddSingleton<IHttpRequester, HttpClientHttpRequester>();
            _services.TryAddSingleton<IHttpResponder, HttpContextResponder>();
            _services.TryAddSingleton<IErrorsToHttpStatusCodeMapper, ErrorsToHttpStatusCodeMapper>();
            _services.TryAddSingleton<IRateLimitCounterHandler, MemoryCacheRateLimitCounterHandler>();
            _services.TryAddSingleton<IHttpClientCache, MemoryHttpClientCache>();
            _services.TryAddSingleton<IRequestMapper, RequestMapper>();
            _services.TryAddSingleton<IHttpHandlerOptionsCreator, HttpHandlerOptionsCreator>();
            _services.TryAddSingleton<IDownstreamAddressesCreator, DownstreamAddressesCreator>();
            _services.TryAddSingleton<IDelegatingHandlerHandlerFactory, DelegatingHandlerHandlerFactory>();

            if (UsingEurekaServiceDiscoveryProvider(configurationRoot))
            {
                _services.AddDiscoveryClient(configurationRoot);
            }
            else
            {
                _services.TryAddSingleton<IDiscoveryClient, FakeEurekaDiscoveryClient>();
            }

            _services.TryAddSingleton<IHttpRequester, HttpClientHttpRequester>();

            // see this for why we register this as singleton http://stackoverflow.com/questions/37371264/invalidoperationexception-unable-to-resolve-service-for-type-microsoft-aspnetc
            // could maybe use a scoped data repository
            _services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            _services.TryAddSingleton<IRequestScopedDataRepository, HttpDataRepository>();
            _services.AddMemoryCache();
            _services.TryAddSingleton<OcelotDiagnosticListener>();

            //add asp.net services..
            var assembly = typeof(FileConfigurationController).GetTypeInfo().Assembly;

            _services.AddMvcCore()
                .AddApplicationPart(assembly)
                .AddControllersAsServices()
                .AddAuthorization()
                .AddJsonFormatters();

            _services.AddLogging();
            _services.AddMiddlewareAnalysis();
            _services.AddWebEncoders();
            _services.AddSingleton<IAdministrationPath>(new NullAdministrationPath());

            _services.TryAddSingleton<IMultiplexer, Multiplexer>();
            _services.TryAddSingleton<IResponseAggregator, SimpleJsonResponseAggregator>();
            _services.AddSingleton<ITracingHandlerFactory, TracingHandlerFactory>();

            // We add this here so that we can always inject something into the factory for IoC..
            _services.AddSingleton<IServiceTracer, FakeServiceTracer>();
            _services.TryAddSingleton<IConsulPollerConfiguration, InMemoryConsulPollerConfiguration>();
            _services.TryAddSingleton<IAddHeadersToResponse, AddHeadersToResponse>();
            _services.TryAddSingleton<IPlaceholders, Placeholders>();
            _services.TryAddSingleton<IConsulClientFactory, ConsulClientFactory>();
            _services.TryAddSingleton<IResponseAggregatorFactory, InMemoryResponseAggregatorFactory>();
            _services.TryAddSingleton<IDefinedAggregatorProvider, ServiceLocatorDefinedAggregatorProvider>();
        }

        public IOcelotAdministrationBuilder AddAdministration(string path, string secret)
        {
            var administrationPath = new AdministrationPath(path);

            //add identity server for admin area
            var identityServerConfiguration = IdentityServerConfigurationCreator.GetIdentityServerConfiguration(secret);

            if (identityServerConfiguration != null)
            {
                AddIdentityServer(identityServerConfiguration, administrationPath);
            }

            var descriptor = new ServiceDescriptor(typeof(IAdministrationPath), administrationPath);
            _services.Replace(descriptor);
            return new OcelotAdministrationBuilder(_services, _configurationRoot);
        }

        public IOcelotAdministrationBuilder AddAdministration(string path, Action<IdentityServerAuthenticationOptions> configureOptions)
        {
            var administrationPath = new AdministrationPath(path);

            if (configureOptions != null)
            {
                AddIdentityServer(configureOptions);
            }

            //todo - hack because we add this earlier so it always exists for some reason...investigate..
            var descriptor = new ServiceDescriptor(typeof(IAdministrationPath), administrationPath);
            _services.Replace(descriptor);
            return new OcelotAdministrationBuilder(_services, _configurationRoot);
        }

        public IOcelotBuilder AddSingletonDefinedAggregator<T>() 
            where T : class, IDefinedAggregator
        {
            _services.AddSingleton<IDefinedAggregator, T>();
            return this;
        }

        public IOcelotBuilder AddTransientDefinedAggregator<T>() 
            where T : class, IDefinedAggregator
        {
            _services.AddTransient<IDefinedAggregator, T>();
            return this;
        }

        public IOcelotBuilder AddSingletonDelegatingHandler<THandler>(bool global = false) 
            where THandler : DelegatingHandler
        {
            if(global)
            {
                _services.AddSingleton<THandler>();
                _services.AddSingleton<GlobalDelegatingHandler>(s => {
                    var service = s.GetService<THandler>();
                    return new GlobalDelegatingHandler(service);
                });
            }
            else
            {
                _services.AddSingleton<DelegatingHandler, THandler>();
            }

            return this;
        }

        public IOcelotBuilder AddTransientDelegatingHandler<THandler>(bool global = false) 
            where THandler : DelegatingHandler 
        {
            if(global)
            {
                _services.AddTransient<THandler>();
                _services.AddTransient<GlobalDelegatingHandler>(s => {
                    var service = s.GetService<THandler>();
                    return new GlobalDelegatingHandler(service);
                });
            }
            else
            {
                _services.AddTransient<DelegatingHandler, THandler>();
            }

            return this;
        }

        public IOcelotBuilder AddOpenTracing(Action<ButterflyOptions> settings)
        {
            // Earlier we add FakeServiceTracer and need to remove it here before we add butterfly
            _services.RemoveAll<IServiceTracer>();
            _services.AddButterfly(settings);   
            return this;
        }

        public IOcelotBuilder AddStoreOcelotConfigurationInConsul()
        {
            _services.AddSingleton<ConsulFileConfigurationPoller>();
            _services.AddSingleton<IFileConfigurationRepository, ConsulFileConfigurationRepository>();
            return this;
        }

        public IOcelotBuilder AddCacheManager(Action<ConfigurationBuilderCachePart> settings)
        {
            var cacheManagerOutputCache = CacheFactory.Build<CachedResponse>("OcelotOutputCache", settings);
            var ocelotOutputCacheManager = new OcelotCacheManagerCache<CachedResponse>(cacheManagerOutputCache);

            _services.RemoveAll(typeof(ICacheManager<CachedResponse>));
            _services.RemoveAll(typeof(IOcelotCache<CachedResponse>));
            _services.AddSingleton<ICacheManager<CachedResponse>>(cacheManagerOutputCache);
            _services.AddSingleton<IOcelotCache<CachedResponse>>(ocelotOutputCacheManager);

            var ocelotConfigCacheManagerOutputCache = CacheFactory.Build<IInternalConfiguration>("OcelotConfigurationCache", settings);
            var ocelotConfigCacheManager = new OcelotCacheManagerCache<IInternalConfiguration>(ocelotConfigCacheManagerOutputCache);
            _services.RemoveAll(typeof(ICacheManager<IInternalConfiguration>));
            _services.RemoveAll(typeof(IOcelotCache<IInternalConfiguration>));
            _services.AddSingleton<ICacheManager<IInternalConfiguration>>(ocelotConfigCacheManagerOutputCache);
            _services.AddSingleton<IOcelotCache<IInternalConfiguration>>(ocelotConfigCacheManager);

            var fileConfigCacheManagerOutputCache = CacheFactory.Build<FileConfiguration>("FileConfigurationCache", settings);
            var fileConfigCacheManager = new OcelotCacheManagerCache<FileConfiguration>(fileConfigCacheManagerOutputCache);
            _services.RemoveAll(typeof(ICacheManager<FileConfiguration>));
            _services.RemoveAll(typeof(IOcelotCache<FileConfiguration>));
            _services.AddSingleton<ICacheManager<FileConfiguration>>(fileConfigCacheManagerOutputCache);
            _services.AddSingleton<IOcelotCache<FileConfiguration>>(fileConfigCacheManager);
            return this;
        }

        private void AddIdentityServer(Action<IdentityServerAuthenticationOptions> configOptions)
        {
            _services
                .AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
                .AddIdentityServerAuthentication(configOptions);
        }

        private void AddIdentityServer(IIdentityServerConfiguration identityServerConfiguration, IAdministrationPath adminPath) 
        {
            _services.TryAddSingleton<IIdentityServerConfiguration>(identityServerConfiguration);
            var identityServerBuilder = _services
                .AddIdentityServer(o => {
                    o.IssuerUri = "Ocelot";
                })
                .AddInMemoryApiResources(Resources(identityServerConfiguration))
                .AddInMemoryClients(Client(identityServerConfiguration));

            var urlFinder = new BaseUrlFinder(_configurationRoot);
            var baseSchemeUrlAndPort = urlFinder.Find();
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();            

            _services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
                .AddIdentityServerAuthentication(o =>
                {
                    o.Authority = baseSchemeUrlAndPort + adminPath.Path;
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

        private List<ApiResource> Resources(IIdentityServerConfiguration identityServerConfiguration)
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
                },
            };
        }

        private List<Client> Client(IIdentityServerConfiguration identityServerConfiguration) 
        {
            return new List<Client>
            {
                new Client
                {
                    ClientId = identityServerConfiguration.ApiName,
                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    ClientSecrets = new List<Secret> {new Secret(identityServerConfiguration.ApiSecret.Sha256())},
                    AllowedScopes = { identityServerConfiguration.ApiName }
                }
            };
        }

        private static bool UsingEurekaServiceDiscoveryProvider(IConfiguration configurationRoot)
        {
            var type = configurationRoot.GetValue<string>("GlobalConfiguration:ServiceDiscoveryProvider:Type",
                string.Empty);

            return type.ToLower() == "eureka";
        }
    }
}
