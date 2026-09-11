using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ocelot.Authorization;
using Ocelot.Cache;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.Configuration.Validator;
using Ocelot.DownstreamRouteFinder.HeaderMatcher;
using Ocelot.Logging;
using Ocelot.QualityOfService;
using Ocelot.RateLimiting;

namespace Ocelot.DependencyInjection;

public static class Features
{
    /// <summary>
    /// Ocelot feature: <see href="https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/authorization.rst">Authorization</see>.
    /// </summary>
    /// <param name="services">The services collection to add the feature to.</param>
    /// <param name="builder">The default builder being returned by <see cref="MvcCoreServiceCollectionExtensions.AddMvcCore(IServiceCollection)"/> extension-method.</param>
    /// <param name="configuration">The exact configuration instance passed to <c>AddOcelot</c>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> object.</returns>
    public static IServiceCollection AddOcelotAuthorization(this IServiceCollection services, IMvcCoreBuilder builder, IConfiguration configuration)
    {
        builder.AddAuthorization(); // TODO Intro AuthorizationOptions (see 2nd constructor)
        return services
            .AddSingleton<IClaimsAuthorizer, ClaimsAuthorizer>()
            .AddSingleton<IScopesAuthorizer, ScopesAuthorizer>()
            .AddSingleton<IPostConfigureOptions<FileConfiguration>>(new RouteClaimsRequirementPostConfigureOptions(configuration));
    }

    /// <summary>This Ocelot Core feature adds validation for JSON configuration File-models.</summary>
    /// <remarks>Added validator-classes must implement the <see cref="AbstractValidator{FileConfiguration}"/> interface, where T is File-model.</remarks>
    /// <param name="services">The services collection to add the feature to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> object.</returns>
    public static IServiceCollection AddOcelotConfigurationValidators(this IServiceCollection services) => services
        .AddSingleton<IConfigurationValidator, FileConfigurationFluentValidator>()
        .AddSingleton<HostAndPortValidator>()
        .AddSingleton<RouteFluentValidator>()
        .AddSingleton<FileGlobalConfigurationFluentValidator>()
        .AddSingleton<FileQoSOptionsFluentValidator>()
        .AddSingleton<FileAuthenticationOptionsValidator>();

    /// <summary>
    /// Adds Ocelot Configuration Repository feature without Configuration Poller (the <see cref="FileConfigurationPoller"/> class).
    /// </summary>
    /// <param name="services">The services collection to add the feature to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> object.</returns>
    public static IServiceCollection AddOcelotConfigurationRepository(this IServiceCollection services) => services
        .AddSingleton<IFileConfigurationRepository, DiskFileConfigurationRepository>()
        .AddSingleton<IFileConfigurationSetter, FileAndInternalConfigurationSetter>()
        .AddSingleton<IInternalConfigurationRepository, InMemoryInternalConfigurationRepository>();

    /// <summary>
    /// Ocelot feature: <see href="https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/ratelimiting.rst">Rate Limiting</see>.
    /// </summary>
    /// <remarks>
    /// Read The Docs: <see href="https://ocelot.readthedocs.io/en/latest/features/ratelimiting.html">Rate Limiting</see>.
    /// </remarks>
    /// <param name="services">The services collection to add the feature to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> object.</returns>
    public static IServiceCollection AddOcelotRateLimiting(this IServiceCollection services) => services
        .AddSingleton<IRateLimiting, RateLimiting.RateLimiting>()
        .AddSingleton<IRateLimitStorage, MemoryCacheRateLimitStorage>();

    /// <summary>
    /// Ocelot feature: <see href="https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/caching.rst">Request Caching</see>.
    /// </summary>
    /// <remarks>
    /// Read The Docs: <see href="https://ocelot.readthedocs.io/en/latest/features/caching.html">Caching</see>.
    /// </remarks>
    /// <param name="services">The services collection to add the feature to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> object.</returns>
    public static IServiceCollection AddOcelotCache(this IServiceCollection services) => services
        .AddSingleton<IOcelotCache<Regex>, DefaultMemoryCache<Regex>>()
        .AddSingleton<IOcelotCache<FileConfiguration>, DefaultMemoryCache<FileConfiguration>>()
        .AddSingleton<IOcelotCache<CachedResponse>, DefaultMemoryCache<CachedResponse>>()
        .AddSingleton<ICacheKeyGenerator, DefaultCacheKeyGenerator>()
        .AddSingleton<ICacheOptionsCreator, CacheOptionsCreator>()
        .AddMemoryCache();

    /// <summary>
    /// Ocelot feature: <see href="https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/routing.rst#upstream-headers">Routing based on request header</see>.
    /// </summary>
    /// <param name="services">The services collection to add the feature to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> object.</returns>
    public static IServiceCollection AddOcelotHeaderRouting(this IServiceCollection services) => services
        .AddSingleton<IUpstreamHeaderTemplatePatternCreator, UpstreamHeaderTemplatePatternCreator>()
        .AddSingleton<IHeadersToHeaderTemplatesMatcher, HeadersToHeaderTemplatesMatcher>()
        .AddSingleton<IHeaderPlaceholderNameAndValueFinder, HeaderPlaceholderNameAndValueFinder>();

    public static IServiceCollection AddOcelotLogging(this IServiceCollection services) => services
        .AddSingleton<IOcelotLoggerFactory, OcelotLoggerFactory>()
        .AddSingleton<OcelotDiagnosticListener>()
        .AddLogging();

    /// <summary>
    /// Ocelot feature: <see href="https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/metadata.rst">Inject custom metadata and use it in delegating handlers</see>.
    /// </summary>
    /// <param name="services">The services collection to add the feature to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> object.</returns>
    public static IServiceCollection AddOcelotMetadata(this IServiceCollection services) => 
        services.AddSingleton<IMetadataCreator, DefaultMetadataCreator>();

    public static IServiceCollection AddOcelotQualityOfService(this IServiceCollection services) => services
        .AddSingleton<IQualityOfServiceFactory, QualityOfServiceFactory>();
}
