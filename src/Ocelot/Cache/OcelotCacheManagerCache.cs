using System;
using CacheManager.Core;

namespace Ocelot.Cache
{
    public class OcelotCacheManagerCache<T> : IOcelotCache<T>
    {
        private readonly ICacheManager<T> _cacheManager;

        public OcelotCacheManagerCache(ICacheManager<T> cacheManager)
        {
            _cacheManager = cacheManager;
        }

        public void Add(string key, T value, TimeSpan ttl)
        {
            _cacheManager.Add(new CacheItem<T>(key, value, ExpirationMode.Absolute, ttl));
        }

        public T Get(string key)
        {
            return _cacheManager.Get<T>(key);
        }
    }
}