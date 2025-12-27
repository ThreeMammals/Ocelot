using CacheManager.Core;

namespace Ocelot.Cache.CacheManager;

public class OcelotCacheManagerCache<T> : IOcelotCache<T>
{
    private readonly ICacheManager<T> _manager;
    public OcelotCacheManagerCache(ICacheManager<T> cacheManager)
    {
        _manager = cacheManager;
    }

    public bool Add(string key, T value, string region, TimeSpan ttl)
    {
        return _manager.Add(new CacheItem<T>(key, region, value, ExpirationMode.Absolute, ttl));
    }

    public T AddOrUpdate(string key, T value, string region, TimeSpan ttl)
    {
        return _manager.AddOrUpdate(key, region, value, v => value);
    }

    public T Get(string key, string region)
    {
        return _manager.Get<T>(key, region);
    }

    public void ClearRegion(string region)
    {
        _manager.ClearRegion(region);
    }

    public bool TryGetValue(string key, string region, out T value)
    {
        var item = _manager.GetCacheItem(key, region);
        value = item != null ? item.Value : default;
        return item != null && !item.IsExpired;
    }
}
