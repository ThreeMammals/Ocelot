using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace Ocelot.RateLimit
{
    public class DistributedCacheRateLimitCounterHandler : IRateLimitCounterHandler
    {
        private readonly IDistributedCache _memoryCache;

        public DistributedCacheRateLimitCounterHandler(IDistributedCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public void Set(string id, RateLimitCounter counter, TimeSpan expirationTime)
        {
            _memoryCache.SetString(id, JsonConvert.SerializeObject(counter), new DistributedCacheEntryOptions().SetAbsoluteExpiration(expirationTime));
        }

        public bool Exists(string id)
        {
            var stored = _memoryCache.GetString(id);
            return !string.IsNullOrEmpty(stored);
        }

        public RateLimitCounter? Get(string id)
        {
            var stored = _memoryCache.GetString(id);
            if (!string.IsNullOrEmpty(stored))
            {
                return JsonConvert.DeserializeObject<RateLimitCounter>(stored);
            }

            return null;
        }

        public void Remove(string id)
        {
            _memoryCache.Remove(id);
        }
    }
}
