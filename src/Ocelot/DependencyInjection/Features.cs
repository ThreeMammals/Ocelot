using Microsoft.Extensions.DependencyInjection;
using Ocelot.Cache;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.DownstreamRouteFinder.HeaderMatcher;
using Ocelot.RateLimiting;

namespace Ocelot.DependencyInjection;

public static class Features
{
    /// <summary>
    /// Ocelot feature: <see href="https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/ratelimiting.rst">Rate Limiting</see>.
    /// </summary>
    /// <remarks>
    /// Read The Docs: <see href="https://ocelot.readthedocs.io/en/latest/features/ratelimiting.html">Rate Limiting</see>.
    /// </remarks>
    /// <param name="services">The services collection to add the feature to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> object.</returns>
    public static IServiceCollection AddRateLimiting(this IServiceCollection services) => services
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
        .AddSingleton<IOcelotCache<FileConfiguration>, DefaultMemoryCache<FileConfiguration>>()
        .AddSingleton<IOcelotCache<CachedResponse>, DefaultMemoryCache<CachedResponse>>()
        .AddSingleton<ICacheKeyGenerator, DefaultCacheKeyGenerator>()
        .AddSingleton<ICacheOptionsCreator, CacheOptionsCreator>();

    /// <summary>
    /// Ocelot feature: <see href="https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/routing.rst#upstream-headers">Routing based on request header</see>.
    /// </summary>
    /// <param name="services">The services collection to add the feature to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> object.</returns>
    public static IServiceCollection AddHeaderRouting(this IServiceCollection services) => services
        .AddSingleton<IUpstreamHeaderTemplatePatternCreator, UpstreamHeaderTemplatePatternCreator>()
        .AddSingleton<IHeadersToHeaderTemplatesMatcher, HeadersToHeaderTemplatesMatcher>()
        .AddSingleton<IHeaderPlaceholderNameAndValueFinder, HeaderPlaceholderNameAndValueFinder>();

    /// <summary>
    /// Ocelot feature: <see href="https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/metadata.rst">Inject custom metadata and use it in delegating handlers</see>.
    /// </summary>
    /// <param name="services">The services collection to add the feature to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> object.</returns>
    public static IServiceCollection AddOcelotMetadata(this IServiceCollection services) => 
        services.AddSingleton<IMetadataCreator, DefaultMetadataCreator>();
}
