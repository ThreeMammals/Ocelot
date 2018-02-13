using CacheManager.Core;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using FileConfigurationProvider = Ocelot.Configuration.Provider.FileConfigurationProvider;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Linq;
using System.Net.Http;
using Butterfly.Client.AspNetCore;

namespace Ocelot.DependencyInjection
{
    public class OcelotBuilder : IOcelotBuilder
    {
        private readonly IServiceCollection _services;
        private readonly IConfiguration _configurationRoot;
        private IDelegatingHandlerHandlerProvider _provider;
        
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
            _services.TryAddSingleton<IOcelotConfigurationCreator, FileOcelotConfigurationCreator>();
            _services.TryAddSingleton<IOcelotConfigurationRepository, InMemoryOcelotConfigurationRepository>();
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
            _services.TryAddSingleton<IFileConfigurationRepository, FileConfigurationRepository>();
            _services.TryAddSingleton<IFileConfigurationSetter, FileConfigurationSetter>();
            _services.TryAddSingleton<IFileConfigurationProvider, FileConfigurationProvider>();
            _services.TryAddSingleton<IQosProviderHouse, QosProviderHouse>();
            _services.TryAddSingleton<IQoSProviderFactory, QoSProviderFactory>();
            _services.TryAddSingleton<IServiceDiscoveryProviderFactory, ServiceDiscoveryProviderFactory>();
            _services.TryAddSingleton<ILoadBalancerFactory, LoadBalancerFactory>();
            _services.TryAddSingleton<ILoadBalancerHouse, LoadBalancerHouse>();
            _services.TryAddSingleton<IOcelotLoggerFactory, AspDotNetLoggerFactory>();
            _services.TryAddSingleton<IUrlBuilder, UrlBuilder>();
            _services.TryAddSingleton<IRemoveOutputHeaders, RemoveOutputHeaders>();
            _services.TryAddSingleton<IOcelotConfigurationProvider, OcelotConfigurationProvider>();
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
            _services.TryAddSingleton<IDownstreamRouteFinder, DownstreamRouteFinder.Finder.DownstreamRouteFinder>();
            _services.TryAddSingleton<IHttpRequester, HttpClientHttpRequester>();
            _services.TryAddSingleton<IHttpResponder, HttpContextResponder>();
            _services.TryAddSingleton<IRequestCreator, HttpRequestCreator>();
            _services.TryAddSingleton<IErrorsToHttpStatusCodeMapper, ErrorsToHttpStatusCodeMapper>();
            _services.TryAddSingleton<IRateLimitCounterHandler, MemoryCacheRateLimitCounterHandler>();
            _services.TryAddSingleton<IHttpClientCache, MemoryHttpClientCache>();
            _services.TryAddSingleton<IRequestMapper, RequestMapper>();
            _services.TryAddSingleton<IHttpHandlerOptionsCreator, HttpHandlerOptionsCreator>();
            _services.TryAddSingleton<IDownstreamAddressesCreator, DownstreamAddressesCreator>();
            _services.TryAddSingleton<IDelegatingHandlerHandlerProviderFactory, DelegatingHandlerHandlerProviderFactory>();
            _services.TryAddSingleton<IDelegatingHandlerHandlerHouse, DelegatingHandlerHandlerHouse>();

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

            //these get picked out later and added to http request
            _provider = new DelegatingHandlerHandlerProvider();
            _services.TryAddSingleton<IDelegatingHandlerHandlerProvider>(_provider);
            _services.AddTransient<ITracingHandler, NoTracingHandler>();
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

        public IOcelotBuilder AddDelegatingHandler(Func<DelegatingHandler> delegatingHandler)
        {
            _provider.Add(delegatingHandler);
            return this;
        }

        public IOcelotBuilder AddOpenTracing(Action<ButterflyOptions> settings)
        {
            _services.AddTransient<ITracingHandler, OcelotHttpTracingHandler>();
            _services.AddButterfly(settings);   
            return this;
        }

        public IOcelotBuilder AddStoreOcelotConfigurationInConsul()
        {
            var serviceDiscoveryPort = _configurationRoot.GetValue("GlobalConfiguration:ServiceDiscoveryProvider:Port", 0);
            var serviceDiscoveryHost = _configurationRoot.GetValue("GlobalConfiguration:ServiceDiscoveryProvider:Host", string.Empty);

            var config = new ServiceProviderConfigurationBuilder()
                .WithServiceDiscoveryProviderPort(serviceDiscoveryPort)
                .WithServiceDiscoveryProviderHost(serviceDiscoveryHost)
                .Build();

            _services.AddSingleton<ServiceProviderConfiguration>(config);
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

            var ocelotConfigCacheManagerOutputCache = CacheFactory.Build<IOcelotConfiguration>("OcelotConfigurationCache", settings);
            var ocelotConfigCacheManager = new OcelotCacheManagerCache<IOcelotConfiguration>(ocelotConfigCacheManagerOutputCache);
            _services.RemoveAll(typeof(ICacheManager<IOcelotConfiguration>));
            _services.RemoveAll(typeof(IOcelotCache<IOcelotConfiguration>));
            _services.AddSingleton<ICacheManager<IOcelotConfiguration>>(ocelotConfigCacheManagerOutputCache);
            _services.AddSingleton<IOcelotCache<IOcelotConfiguration>>(ocelotConfigCacheManager);

            var fileConfigCacheManagerOutputCache = CacheFactory.Build<FileConfiguration>("FileConfigurationCache", settings);
            var fileConfigCacheManager = new OcelotCacheManagerCache<FileConfiguration>(fileConfigCacheManagerOutputCache);
            _services.RemoveAll(typeof(ICacheManager<FileConfiguration>));
            _services.RemoveAll(typeof(IOcelotCache<FileConfiguration>));
            _services.AddSingleton<ICacheManager<FileConfiguration>>(fileConfigCacheManagerOutputCache);
            _services.AddSingleton<IOcelotCache<FileConfiguration>>(fileConfigCacheManager);
            return this;
        }

        private void AddIdentityServer(IIdentityServerConfiguration identityServerConfiguration, IAdministrationPath adminPath) 
        {
            _services.TryAddSingleton<IIdentityServerConfiguration>(identityServerConfiguration);
            _services.TryAddSingleton<IHashMatcher, HashMatcher>();
            var identityServerBuilder = _services
                .AddIdentityServer(o => {
                    o.IssuerUri = "Ocelot";
                })
                .AddInMemoryApiResources(Resources(identityServerConfiguration))
                .AddInMemoryClients(Client(identityServerConfiguration));

            //todo - refactor a method so we know why this is happening
            var whb = _services.First(x => x.ServiceType == typeof(IWebHostBuilder));
            var urlFinder = new BaseUrlFinder((IWebHostBuilder)whb.ImplementationInstance);
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
    }
}
