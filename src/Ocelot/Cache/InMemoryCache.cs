namespace Ocelot.Cache
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    public class InMemoryCache<T> : IOcelotCache<T>
    {
        private readonly ConcurrentDictionary<string, CacheObject<T>> _cache;
        private readonly ConcurrentDictionary<string, List<string>> _regions;

        public InMemoryCache()
        {
            _cache = new ConcurrentDictionary<string, CacheObject<T>>();
            _regions = new ConcurrentDictionary<string, List<string>>();
        }

        public void Add(string key, T value, TimeSpan ttl, string region)
        {
            if (ttl.TotalMilliseconds <= 0)
            {
                return;
            }

            var expires = DateTime.UtcNow.Add(ttl);

            var cacheObject = new CacheObject<T>(value, expires);

            _cache.AddOrUpdate(key, cacheObject, (x, y) => cacheObject);

            if (_regions.ContainsKey(region))
            {
                var current = _regions[region];
                if (!current.Contains(key))
                {
                    current.Add(key);
                }
            }
            else
            {
                var keys = new List<string> { key };
                _regions.AddOrUpdate(region, keys, (x, y) => keys);
            }
        }

        public void AddAndDelete(string key, T value, TimeSpan ttl, string region)
        {
            if (_cache.ContainsKey(key))
            {
                _cache.TryRemove(key, out _);
            }

            Add(key, value, ttl, region);
        }

        public void ClearRegion(string region)
        {
            if (_regions.ContainsKey(region))
            {
                var keys = _regions[region];
                foreach (var key in keys)
                {
                    _cache.TryRemove(key, out _);
                }
            }
        }

        public T Get(string key, string region)
        {
            if (_cache.ContainsKey(key))
            {
                var cached = _cache[key];

                if (cached.Expires > DateTime.UtcNow)
                {
                    return cached.Value;
                }

                _cache.TryRemove(key, out _);
            }

            return default(T);
        }
    }
}
