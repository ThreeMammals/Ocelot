using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Ocelot.Authorization;
using Ocelot.Claims;
using Ocelot.Configuration;
using Ocelot.Configuration.ChangeTracking;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Parser;
using Ocelot.Configuration.Repository;
using Ocelot.Configuration.Setter;
using Ocelot.Configuration.Validator;
using Ocelot.DownstreamRouteFinder.Finder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.DownstreamUrlCreator;
using Ocelot.Headers;
using Ocelot.Infrastructure;
using Ocelot.Infrastructure.Claims.Parser;
using Ocelot.Infrastructure.RequestData;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Multiplexer;
using Ocelot.PathManipulation;
using Ocelot.QueryStrings;
using Ocelot.Request.Creator;
using Ocelot.Request.Mapper;
using Ocelot.Requester;
using Ocelot.Requester.QoS;
using Ocelot.Responder;
using Ocelot.Security;
using Ocelot.Security.IPSecurity;
using Ocelot.ServiceDiscovery;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.WebSockets;
using System.Reflection;

namespace Ocelot.DependencyInjection;

public class OcelotBuilder : IOcelotBuilder
{
    public IServiceCollection Services { get; }
    public IConfiguration Configuration { get; }
    public IMvcCoreBuilder MvcCoreBuilder { get; }

    public OcelotBuilder(IServiceCollection services, IConfiguration configurationRoot, Func<IMvcCoreBuilder, Assembly, IMvcCoreBuilder> customBuilder = null)
    {
        Configuration = configurationRoot;
        Services = services;
        Services.Configure<FileConfiguration>(configurationRoot);
        Services.Configure<FileGlobalConfiguration>(configurationRoot.GetSection(nameof(FileConfiguration.GlobalConfiguration)));

        Services.TryAddSingleton<IHttpResponseHeaderReplacer, HttpResponseHeaderReplacer>();
        Services.TryAddSingleton<IHttpContextRequestHeaderReplacer, HttpContextRequestHeaderReplacer>();
        Services.TryAddSingleton<IHeaderFindAndReplaceCreator, HeaderFindAndReplaceCreator>();
        Services.TryAddSingleton<IInternalConfigurationCreator, FileInternalConfigurationCreator>();
        Services.TryAddSingleton<IInternalConfigurationRepository, InMemoryInternalConfigurationRepository>();
        Services.TryAddSingleton<IConfigurationValidator, FileConfigurationFluentValidator>();
        Services.TryAddSingleton<HostAndPortValidator>();
        Services.TryAddSingleton<IRoutesCreator, RoutesCreator>();
        Services.TryAddSingleton<IAggregatesCreator, AggregatesCreator>();
        Services.TryAddSingleton<IRouteKeyCreator, RouteKeyCreator>();
        Services.TryAddSingleton<IConfigurationCreator, ConfigurationCreator>();
        Services.TryAddSingleton<IDynamicsCreator, DynamicsCreator>();
        Services.TryAddSingleton<ILoadBalancerOptionsCreator, LoadBalancerOptionsCreator>();
        Services.TryAddSingleton<RouteFluentValidator>();
        Services.TryAddSingleton<FileGlobalConfigurationFluentValidator>();
        Services.TryAddSingleton<FileQoSOptionsFluentValidator>();
        Services.TryAddSingleton<IClaimsToThingCreator, ClaimsToThingCreator>();
        Services.TryAddSingleton<IAuthenticationOptionsCreator, AuthenticationOptionsCreator>();
        Services.TryAddSingleton<IUpstreamTemplatePatternCreator, UpstreamTemplatePatternCreator>();
        Services.TryAddSingleton<IRequestIdKeyCreator, RequestIdKeyCreator>();
        Services.TryAddSingleton<IServiceProviderConfigurationCreator, ServiceProviderConfigurationCreator>();
        Services.TryAddSingleton<IQoSOptionsCreator, QoSOptionsCreator>();
        Services.TryAddSingleton<IRouteOptionsCreator, RouteOptionsCreator>();
        Services.TryAddSingleton<IRateLimitOptionsCreator, RateLimitOptionsCreator>();
        Services.TryAddSingleton<IBaseUrlFinder, BaseUrlFinder>();
        Services.TryAddSingleton<IFileConfigurationRepository, DiskFileConfigurationRepository>();
        Services.TryAddSingleton<IFileConfigurationSetter, FileAndInternalConfigurationSetter>();
        Services.TryAddSingleton<IServiceDiscoveryProviderFactory, ServiceDiscoveryProviderFactory>();
        Services.AddSingleton<ILoadBalancerCreator, NoLoadBalancerCreator>();
        Services.AddSingleton<ILoadBalancerCreator, RoundRobinCreator>();
        Services.AddSingleton<ILoadBalancerCreator, CookieStickySessionsCreator>();
        Services.AddSingleton<ILoadBalancerCreator, LeastConnectionCreator>();
        Services.TryAddSingleton<ILoadBalancerFactory, LoadBalancerFactory>();
        Services.TryAddSingleton<ILoadBalancerHouse, LoadBalancerHouse>();
        Services.TryAddSingleton<IOcelotLoggerFactory, OcelotLoggerFactory>();
        Services.TryAddSingleton<IRemoveOutputHeaders, RemoveOutputHeaders>();
        Services.TryAddSingleton<IClaimToThingConfigurationParser, ClaimToThingConfigurationParser>();
        Services.TryAddSingleton<IClaimsAuthorizer, ClaimsAuthorizer>();
        Services.TryAddSingleton<IScopesAuthorizer, ScopesAuthorizer>();
        Services.TryAddSingleton<IAddClaimsToRequest, AddClaimsToRequest>();
        Services.TryAddSingleton<IAddHeadersToRequest, AddHeadersToRequest>();
        Services.TryAddSingleton<IAddQueriesToRequest, AddQueriesToRequest>();
        Services.TryAddSingleton<IChangeDownstreamPathTemplate, ChangeDownstreamPathTemplate>();
        Services.TryAddSingleton<IClaimsParser, ClaimsParser>();
        Services.TryAddSingleton<IUrlPathToUrlTemplateMatcher, RegExUrlMatcher>();
        Services.TryAddSingleton<IPlaceholderNameAndValueFinder, UrlPathPlaceholderNameAndValueFinder>();
        Services.TryAddSingleton<IDownstreamPathPlaceholderReplacer, DownstreamPathPlaceholderReplacer>();
        Services.AddSingleton<IDownstreamRouteProvider, DownstreamRouteFinder.Finder.DownstreamRouteFinder>();
        Services.AddSingleton<IDownstreamRouteProvider, DownstreamRouteCreator>();
        Services.TryAddSingleton<IDownstreamRouteProviderFactory, DownstreamRouteProviderFactory>();
        Services.TryAddSingleton<IHttpResponder, HttpContextResponder>();
        Services.TryAddSingleton<IErrorsToHttpStatusCodeMapper, ErrorsToHttpStatusCodeMapper>();
        Services.AddRateLimiting(); // Feature: Rate Limiting
        Services.TryAddSingleton<IRequestMapper, RequestMapper>();
        Services.TryAddSingleton<IHttpHandlerOptionsCreator, HttpHandlerOptionsCreator>();
        Services.TryAddSingleton<IDownstreamAddressesCreator, DownstreamAddressesCreator>();
        Services.TryAddSingleton<IDelegatingHandlerFactory, DelegatingHandlerFactory>();
        
        Services.TryAddSingleton<IOcelotConfigurationChangeTokenSource, OcelotConfigurationChangeTokenSource>();
        Services.TryAddSingleton<IOptionsMonitor<IInternalConfiguration>, OcelotConfigurationMonitor>();

        Services.AddOcelotCache();
        Services.AddOcelotMetadata();
        Services.AddOcelotMessageInvokerPool();

        // Chinese developers should read StackOverflow ignoring Microsoft Learn docs -> http://stackoverflow.com/questions/37371264/invalidoperationexception-unable-to-resolve-service-for-type-microsoft-aspnetc
        Services.AddHttpContextAccessor();
        Services.TryAddSingleton<IRequestScopedDataRepository, HttpDataRepository>();
        Services.AddMemoryCache();
        Services.TryAddSingleton<OcelotDiagnosticListener>();
        Services.TryAddSingleton<IResponseAggregator, SimpleJsonResponseAggregator>();
        Services.TryAddSingleton<ITracingHandlerFactory, TracingHandlerFactory>();
        Services.TryAddSingleton<IFileConfigurationPollerOptions, InMemoryFileConfigurationPollerOptions>();
        Services.TryAddSingleton<IAddHeadersToResponse, AddHeadersToResponse>();
        Services.TryAddSingleton<IPlaceholders, Placeholders>();
        Services.TryAddSingleton<IResponseAggregatorFactory, InMemoryResponseAggregatorFactory>();
        Services.TryAddSingleton<IDefinedAggregatorProvider, ServiceLocatorDefinedAggregatorProvider>();
        Services.TryAddSingleton<IDownstreamRequestCreator, DownstreamRequestCreator>();
        Services.TryAddSingleton<IFrameworkDescription, FrameworkDescription>();
        Services.TryAddSingleton<IQoSFactory, QoSFactory>();
        Services.TryAddSingleton<IExceptionToErrorMapper, HttpExceptionToErrorMapper>();
        Services.TryAddSingleton<IVersionCreator, HttpVersionCreator>();
        Services.TryAddSingleton<IVersionPolicyCreator, HttpVersionPolicyCreator>();
        Services.TryAddSingleton<IWebSocketsFactory, WebSocketsFactory>();

        // Add security
        Services.TryAddSingleton<ISecurityOptionsCreator, SecurityOptionsCreator>();
        Services.TryAddSingleton<ISecurityPolicy, IPSecurityPolicy>();

        // Features
        Services.AddHeaderRouting();

        // Add ASP.NET services
        var assembly = typeof(FileConfigurationController).GetTypeInfo().Assembly;
        MvcCoreBuilder = (customBuilder ?? AddDefaultAspNetServices)
            .Invoke(Services.AddMvcCore(), assembly);
    }

    /// <summary>
    /// Adds default ASP.NET services which are the minimal part of the gateway core.
    /// <para>
    /// Finally the builder adds Newtonsoft.Json services via the <see cref="NewtonsoftJsonMvcCoreBuilderExtensions.AddNewtonsoftJson(IMvcCoreBuilder)"/> extension-method.<br/>
    /// To remove these services, use custom builder in the <see cref="ServiceCollectionExtensions.AddOcelotUsingBuilder(IServiceCollection, Func{IMvcCoreBuilder, Assembly, IMvcCoreBuilder})"/> extension-method.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Note that the following <see cref="IServiceCollection"/> extensions being called:<br/>
    /// - <see cref="MvcCoreServiceCollectionExtensions.AddMvcCore(IServiceCollection)"/>, impossible to remove.<br/>
    /// - <see cref="LoggingServiceCollectionExtensions.AddLogging(IServiceCollection)"/><br/>
    /// - <see cref="AnalysisServiceCollectionExtensions.AddMiddlewareAnalysis(IServiceCollection)"/><br/>
    /// - <see cref="EncoderServiceCollectionExtensions.AddWebEncoders(IServiceCollection)"/>.
    /// <para>
    /// Warning! The following <see cref="IMvcCoreBuilder"/> extensions being called:<br/>
    /// - <see cref="MvcCoreMvcCoreBuilderExtensions.AddApplicationPart(IMvcCoreBuilder, Assembly)"/><br/>
    /// - <see cref="MvcCoreMvcCoreBuilderExtensions.AddControllersAsServices(IMvcCoreBuilder)"/><br/>
    /// - <see cref="MvcCoreMvcCoreBuilderExtensions.AddAuthorization(IMvcCoreBuilder)"/><br/>
    /// - <see cref="NewtonsoftJsonMvcCoreBuilderExtensions.AddNewtonsoftJson(IMvcCoreBuilder)"/>, removable.
    /// </para>
    /// </remarks>
    /// <param name="builder">The default builder being returned by <see cref="MvcCoreServiceCollectionExtensions.AddMvcCore(IServiceCollection)"/> extension-method.</param>
    /// <param name="assembly">The web app assembly.</param>
    /// <returns>An <see cref="IMvcCoreBuilder"/> object.</returns>
    protected IMvcCoreBuilder AddDefaultAspNetServices(IMvcCoreBuilder builder, Assembly assembly)
    {
        Services
            .AddLogging()
            .AddMiddlewareAnalysis()
            .AddWebEncoders();

        return builder
            .AddApplicationPart(assembly)
            .AddControllersAsServices()
            .AddAuthorization()
            .AddNewtonsoftJson();
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

    public IOcelotBuilder AddCustomLoadBalancer<TLoadBalancer>()
        where TLoadBalancer : ILoadBalancer, new()
    {
        static TLoadBalancer Create(IServiceProvider provider, DownstreamRoute route, IServiceDiscoveryProvider discoveryProvider)
            => new(); // TODO Not tested by acceptance tests, Assert another constructors with injected params?
        return AddCustomLoadBalancer<TLoadBalancer>(Create);
    }

    public IOcelotBuilder AddCustomLoadBalancer<TLoadBalancer>(Func<TLoadBalancer> loadBalancerFactoryFunc)
        where TLoadBalancer : ILoadBalancer
    {
        TLoadBalancer Create(IServiceProvider provider, DownstreamRoute route, IServiceDiscoveryProvider discoveryProvider)
            => loadBalancerFactoryFunc();
        return AddCustomLoadBalancer<TLoadBalancer>(Create);
    }

    public IOcelotBuilder AddCustomLoadBalancer<TLoadBalancer>(Func<IServiceProvider, TLoadBalancer> loadBalancerFactoryFunc)
        where TLoadBalancer : ILoadBalancer
    {
        TLoadBalancer Create(IServiceProvider provider, DownstreamRoute route, IServiceDiscoveryProvider discoveryProvider)
            => loadBalancerFactoryFunc(provider);
        return AddCustomLoadBalancer<TLoadBalancer>(Create);
    }

    public IOcelotBuilder AddCustomLoadBalancer<TLoadBalancer>(Func<DownstreamRoute, IServiceDiscoveryProvider, TLoadBalancer> loadBalancerFactoryFunc)
        where TLoadBalancer : ILoadBalancer
    {
        TLoadBalancer Create(IServiceProvider provider, DownstreamRoute route, IServiceDiscoveryProvider discoveryProvider)
            => loadBalancerFactoryFunc(route, discoveryProvider);
        return AddCustomLoadBalancer<TLoadBalancer>(Create);
    }

    public IOcelotBuilder AddCustomLoadBalancer<TLoadBalancer>(Func<IServiceProvider, DownstreamRoute, IServiceDiscoveryProvider, TLoadBalancer> loadBalancerFactoryFunc)
        where TLoadBalancer : ILoadBalancer
    {
        ILoadBalancer Create(DownstreamRoute route, IServiceDiscoveryProvider discoveryProvider)
            => loadBalancerFactoryFunc(_serviceProvider, route, discoveryProvider);
        ILoadBalancerCreator implementationFactory(IServiceProvider provider)
        {
            _serviceProvider = provider;
            return new DelegateInvokingLoadBalancerCreator<TLoadBalancer>(Create);
        }

        Services.AddSingleton<ILoadBalancerCreator>(implementationFactory);
        return this;
    }

    /// <summary>
    /// Adds a <see cref="DelegatingHandler"/> of the <paramref name="delegateType"/> type as a transient service, with the <paramref name="global"/> option to make the handler globally available.
    /// </summary>
    /// <param name="delegateType">The type of a <see cref="DelegatingHandler"/> to be registered.</param>
    /// <param name="global">True if the handler should be globally available.</param>
    /// <returns>The reference to the same <see cref="IOcelotBuilder"/> object.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Generates an exception if the <paramref name="delegateType"/> type does not inherit from the <see cref="DelegatingHandler"/>.</exception>
    public IOcelotBuilder AddDelegatingHandler(Type delegateType, bool global = false)
    {
        if (!typeof(DelegatingHandler).IsAssignableFrom(delegateType))
        {
            throw new ArgumentOutOfRangeException(nameof(delegateType), delegateType.Name, "It is not a delegating handler");
        }

        if (global)
        {
            Services.AddTransient(delegateType);
            Services.AddTransient(provider =>
            {
                var service = provider.GetService(delegateType) as DelegatingHandler;
                return new GlobalDelegatingHandler(service);
            });
        }
        else
        {
            Services.AddTransient(typeof(DelegatingHandler), delegateType);
        }

        return this;
    }

    /// <summary>
    /// Adds a <see cref="DelegatingHandler"/> of the <typeparamref name="THandler"/> type as a transient service, with the <paramref name="global"/> option to make the handler globally available.
    /// </summary>
    /// <typeparam name="THandler">The type of a <see cref="DelegatingHandler"/> to be registered.</typeparam>
    /// <param name="global">True if the handler should be globally available.</param>
    /// <returns>The reference to the same <see cref="IOcelotBuilder"/> object.</returns>
    public IOcelotBuilder AddDelegatingHandler<THandler>(bool global = false)
        where THandler : DelegatingHandler
    {
        if (global)
        {
            Services.AddTransient<THandler>();
            Services.AddTransient(provider =>
            {
                var service = provider.GetService<THandler>();
                return new GlobalDelegatingHandler(service);
            });
        }
        else
        {
            Services.AddTransient<DelegatingHandler, THandler>();
        }

        return this;
    }

    public IOcelotBuilder AddConfigPlaceholders()
    {
        // see: https://greatrexpectations.com/2018/10/25/decorators-in-net-core-with-dependency-injection
        var wrappedDescriptor = Services.First(x => x.ServiceType == typeof(IPlaceholders));

        var objectFactory = ActivatorUtilities.CreateFactory(
            typeof(ConfigAwarePlaceholders),
            new[] { typeof(IPlaceholders) });

        Services.Replace(ServiceDescriptor.Describe(
            typeof(IPlaceholders),
            provider => (IPlaceholders)objectFactory(
                provider,
                new[] { CreateInstance(provider, wrappedDescriptor) }),
            wrappedDescriptor.Lifetime
        ));

        return this;
    }

    /// <summary>For local implementation purposes, so it MUST NOT be public!..</summary>
    private IServiceProvider _serviceProvider; // TODO Reuse ActivatorUtilities factories?

    private static object CreateInstance(IServiceProvider provider, ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationInstance != null)
        {
            return descriptor.ImplementationInstance;
        }

        if (descriptor.ImplementationFactory != null)
        {
            return descriptor.ImplementationFactory(provider);
        }

        return ActivatorUtilities.GetServiceOrCreateInstance(provider, descriptor.ImplementationType);
    }
}
