﻿using CacheManager.Core;

namespace Ocelot.Cache.CacheManager;

public class OcelotCacheManagerCache<T> : IOcelotCache<T>
{
    private readonly ICacheManager<T> _cacheManager;

    public OcelotCacheManagerCache(ICacheManager<T> cacheManager)
    {
        _cacheManager = cacheManager;
    }

    public void Add(string key, T value, TimeSpan ttl, string region)
    {
        _cacheManager.Add(new CacheItem<T>(key, region, value, ExpirationMode.Absolute, ttl));
    }

    public void AddAndDelete(string key, T value, TimeSpan ttl, string region)
    {
        var exists = _cacheManager.Get(key);

        if (exists != null)
        {
            _cacheManager.Remove(key);
        }

        Add(key, value, ttl, region);
    }

    public T Get(string key, string region)
    {
        return _cacheManager.Get<T>(key, region);
    }

    public void ClearRegion(string region)
    {
        _cacheManager.ClearRegion(region);
    }

    public bool TryGetValue(string key, string region, out T value)
    {
        return _cacheManager.TryGetOrAdd(key, region, (a, b) => default, out value);
    }
}
