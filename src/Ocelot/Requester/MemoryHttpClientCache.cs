using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Ocelot.Requester
{
    public class MemoryHttpClientCache : IHttpClientCache
    {
        private readonly IMemoryCache _memoryCache;

        public MemoryHttpClientCache(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public void Set(string id, IHttpClient client, TimeSpan expirationTime)
        {
            _memoryCache.Set(id, client, new MemoryCacheEntryOptions().SetAbsoluteExpiration(expirationTime));
        }

        public bool Exists(string id)
        {
            IHttpClient counter;
            return _memoryCache.TryGetValue(id, out counter);
        }

        public IHttpClient Get(string id)
        {
            IHttpClient counter;
            if (_memoryCache.TryGetValue(id, out counter))
            {
                return counter;
            }

            return null;
        }

        public void Remove(string id)
        {
            _memoryCache.Remove(id);
        }
    }
}
