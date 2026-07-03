using Microsoft.Extensions.Caching.Memory;
using Ocelot.Infrastructure.Extensions;

namespace Ocelot.Cache;

public class DefaultMemoryCache<T> : IOcelotCache<T>
{
    private readonly IMemoryCache _memoryCache;
    private readonly ConcurrentDictionary<string, ConcurrentBag<string>> _regions;

    public DefaultMemoryCache(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
        _regions = new();
    }

    public bool Add(string key, T value, string region, TimeSpan ttl)
    {
        if (ttl.TotalMilliseconds <= 0)
        {
            return false;
        }

        _memoryCache.Set(key, value, ttl);
        SetRegion(region, key);
        return true;
    }

    public T AddOrUpdate(string key, T value, string region, TimeSpan ttl)
    {
        if (_memoryCache.TryGetValue(key, out _))
        {
            _memoryCache.Remove(key);
        }

        Add(key, value, region, ttl);
        return value;
    }

    public T Get(string key, string region)
    {
        if (TryGetValue(key, region, out T value))
        {
            return value;
        }

        return default;
    }

    public void ClearRegion(string region)
    {
        if (region.IsNotEmpty() && _regions.TryGetValue(region, out var keys))
        {
            foreach (var key in keys)
            {
                _memoryCache.Remove(key);
            }

            keys.Clear();
        }
    }

    private void SetRegion(string region, string key)
    {
        if (region.IsNotEmpty() && _regions.TryGetValue(region, out var current))
        {
            if (key.IsNotEmpty() && !current.Contains(key))
            {
                current.Add(key);
            }
        }
        else if (region.IsNotEmpty())
        {
            _regions.TryAdd(region, [key]);
        }
    }

    public bool TryGetValue(string key, string region, out T value)
    {
        return _memoryCache.TryGetValue(key, out value);
    }
}
