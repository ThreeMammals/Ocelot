using CacheManager.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;

namespace Ocelot.Cache.CacheManager
{
    public static class OcelotBuilderExtensions
    {
        public static IOcelotBuilder AddCacheManager(this IOcelotBuilder builder, Action<ConfigurationBuilderCachePart> settings)
        {
            var cacheManagerOutputCache = CacheFactory.Build<CachedResponse>("OcelotOutputCache", settings);
            var ocelotOutputCacheManager = new OcelotCacheManagerCache<CachedResponse>(cacheManagerOutputCache);

            builder.Services.RemoveAll(typeof(ICacheManager<CachedResponse>));
            builder.Services.RemoveAll(typeof(IOcelotCache<CachedResponse>));
            builder.Services.AddSingleton(cacheManagerOutputCache);
            builder.Services.AddSingleton<IOcelotCache<CachedResponse>>(ocelotOutputCacheManager);

            var ocelotConfigCacheManagerOutputCache = CacheFactory.Build<IInternalConfiguration>("OcelotConfigurationCache", settings);
            var ocelotConfigCacheManager = new OcelotCacheManagerCache<IInternalConfiguration>(ocelotConfigCacheManagerOutputCache);
            builder.Services.RemoveAll(typeof(ICacheManager<IInternalConfiguration>));
            builder.Services.RemoveAll(typeof(IOcelotCache<IInternalConfiguration>));
            builder.Services.AddSingleton(ocelotConfigCacheManagerOutputCache);
            builder.Services.AddSingleton<IOcelotCache<IInternalConfiguration>>(ocelotConfigCacheManager);

            var fileConfigCacheManagerOutputCache = CacheFactory.Build<FileConfiguration>("FileConfigurationCache", settings);
            var fileConfigCacheManager = new OcelotCacheManagerCache<FileConfiguration>(fileConfigCacheManagerOutputCache);
            builder.Services.RemoveAll(typeof(ICacheManager<FileConfiguration>));
            builder.Services.RemoveAll(typeof(IOcelotCache<FileConfiguration>));
            builder.Services.AddSingleton(fileConfigCacheManagerOutputCache);
            builder.Services.AddSingleton<IOcelotCache<FileConfiguration>>(fileConfigCacheManager);

            builder.Services.RemoveAll(typeof(ICacheKeyGenerator));
            builder.Services.AddSingleton<ICacheKeyGenerator, DefaultCacheKeyGenerator>();

            return builder;
        }
    }
}
