using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Ocelot.Requester
{
    public class MemoryHttpClientMessageCacheHandler : IHttpClientMessageCacheHandler
    {
        private readonly IMemoryCache _memoryCache;

        public MemoryHttpClientMessageCacheHandler(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public void Set(string id, IHttpClient counter, TimeSpan expirationTime)
        {
            _memoryCache.Set(id, counter, new MemoryCacheEntryOptions().SetAbsoluteExpiration(expirationTime));
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
