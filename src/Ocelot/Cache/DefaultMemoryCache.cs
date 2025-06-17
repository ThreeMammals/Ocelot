using Microsoft.Extensions.Caching.Memory;

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

    public void Add(string key, T value, TimeSpan ttl, string region)
    {
        if (ttl.TotalMilliseconds <= 0)
        {
            return;
        }

        _memoryCache.Set(key, value, ttl);
        SetRegion(region, key);
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
        if (_regions.TryGetValue(region, out var keys))
        {
            foreach (var key in keys)
            {
                _memoryCache.Remove(key);
            }

            keys.Clear();
        }
    }

    public void AddAndDelete(string key, T value, TimeSpan ttl, string region)
    {
        if (_memoryCache.TryGetValue(key, out T _))
        {
            _memoryCache.Remove(key);
        }

        Add(key, value, ttl, region);
    }

    private void SetRegion(string region, string key)
    {
        if (_regions.TryGetValue(region, out var current))
        {
            if (!current.Contains(key))
            {
                current.Add(key);
            }
        }
        else
        {
            _regions.TryAdd(region, new() { key });
        }
    }

    public bool TryGetValue(string key, string region, out T value)
    {
        return _memoryCache.TryGetValue(key, out value);
    }
}
