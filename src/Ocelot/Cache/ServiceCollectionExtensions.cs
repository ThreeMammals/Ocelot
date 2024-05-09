using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;

namespace Ocelot.Cache;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Extension method used to add Ocelot cache services to the DI container.
    /// </summary>
    /// <param name="services">The services collection.</param>
    public static void AddOcelotCache(this IServiceCollection services)
    {
        services.AddSingleton<IOcelotCache<FileConfiguration>, DefaultMemoryCache<FileConfiguration>>();
        services.AddSingleton<IOcelotCache<CachedResponse>, DefaultMemoryCache<CachedResponse>>();
        services.AddSingleton<ICacheKeyGenerator, DefaultCacheKeyGenerator>();
        services.AddSingleton<IRegionCreator, RegionCreator>();
        services.AddSingleton<ICacheOptionsCreator, CacheOptionsCreator>();
    }
}
