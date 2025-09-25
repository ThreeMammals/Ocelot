using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Cache;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Validator;
using Ocelot.DownstreamRouteFinder.HeaderMatcher;
using Ocelot.RateLimiting;
using FluentValidation;

#if NET7_0_OR_GREATER
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
#endif

namespace Ocelot.DependencyInjection;

public static class Features
{
    /// <summary>This Ocelot Core feature adds validation for JSON configuration File-models.</summary>
    /// <remarks>Added validator-classes must implement the <see cref="AbstractValidator{FileConfiguration}"/> interface, where T is File-model.</remarks>
    /// <param name="services">The services collection to add the feature to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> object.</returns>
    public static IServiceCollection AddConfigurationValidators(this IServiceCollection services) => services
        .AddSingleton<IConfigurationValidator, FileConfigurationFluentValidator>()
        .AddSingleton<HostAndPortValidator>()
        .AddSingleton<RouteFluentValidator>()
        .AddSingleton<FileGlobalConfigurationFluentValidator>()
        .AddSingleton<FileQoSOptionsFluentValidator>()
        .AddSingleton<FileAuthenticationOptionsValidator>();

    /// <summary>
    /// Ocelot feature: <see href="https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/ratelimiting.rst">Rate Limiting</see>.
    /// </summary>
    /// <remarks>
    /// Read The Docs: <see href="https://ocelot.readthedocs.io/en/latest/features/ratelimiting.html">Rate Limiting</see>.
    /// </remarks>
    /// <param name="services">The services collection to add the feature to.</param>
    /// <param name="configurationRoot">Root configuration object.</param>
    /// <returns>The same <see cref="IServiceCollection"/> object.</returns>
    public static IServiceCollection AddRateLimiting(this IServiceCollection services) => services
        .AddSingleton<IRateLimiting, RateLimiting.RateLimiting>()
        .AddSingleton<IRateLimitStorage, MemoryCacheRateLimitStorage>();
    
#if NET7_0_OR_GREATER
    /// <summary>
    /// Ocelot feature: <see href="">AspNet Rate Limiting</see>.
    /// </summary>
    /// <remarks>
    /// Read The Docs: <see href="">Rate Limiting</see>.
    /// </remarks>
    /// <param name="services">The services collection to add the feature to.</param>
    /// <param name="configurationRoot">Root configuration object.</param>
    /// <returns>The same <see cref="IServiceCollection"/> object.</returns>
    public static IServiceCollection AddAspNetRateLimiting(this IServiceCollection services, IConfiguration configurationRoot)
    {
        var globalRateLimitOptions = configurationRoot.Get<FileConfiguration>()?.GlobalConfiguration?.RateLimitOptions;
        var rejectStatusCode = globalRateLimitOptions?.HttpStatusCode ?? StatusCodes.Status429TooManyRequests;
        var rejectedMessage = globalRateLimitOptions?.QuotaExceededMessage ?? "API calls quota exceeded!";
        services.AddRateLimiter(options =>
        {
            options.OnRejected = async (rejectedContext, token) =>
            {
                rejectedContext.HttpContext.Response.StatusCode = rejectStatusCode;
                await rejectedContext.HttpContext.Response.WriteAsync(rejectedMessage, token);
            };
        });

        return services;
    }
#endif

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
