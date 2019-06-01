namespace Ocelot.Cache
{
    using System;
    using System.Collections.Generic;

    public class InMemoryCache<T> : IOcelotCache<T>
    {
        private readonly Dictionary<string, CacheObject<T>> _cache;
        private readonly Dictionary<string, List<string>> _regions;

        public InMemoryCache()
        {
            _cache = new Dictionary<string, CacheObject<T>>();
            _regions = new Dictionary<string, List<string>>();
        }

        public void Add(string key, T value, TimeSpan ttl, string region)
        {
            if (ttl.TotalMilliseconds <= 0)
            {
                return;
            }

            var expires = DateTime.UtcNow.Add(ttl);

            _cache.Add(key, new CacheObject<T>(value, expires));

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
                _regions.Add(region, new List<string> { key });
            }
        }

        public void AddAndDelete(string key, T value, TimeSpan ttl, string region)
        {
            if (_cache.ContainsKey(key))
            {
                _cache.Remove(key);
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
                    _cache.Remove(key);
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

                _cache.Remove(key);
            }

            return default(T);
        }
    }
}
