namespace Ocelot.Requester
{
    using Configuration;
    using System;
    using System.Collections.Concurrent;

    public class MemoryHttpClientCache : IHttpClientCache
    {
        private readonly ConcurrentDictionary<DownstreamReRoute, IHttpClient> _httpClientsCache;

        public MemoryHttpClientCache()
        {
            _httpClientsCache = new ConcurrentDictionary<DownstreamReRoute, IHttpClient>();
        }

        public void Set(DownstreamReRoute key, IHttpClient client, TimeSpan expirationTime)
        {
            _httpClientsCache.AddOrUpdate(key, client, (k, oldValue) => client);
        }

        public IHttpClient Get(DownstreamReRoute key)
        {
            //todo handle error?
            return _httpClientsCache.TryGetValue(key, out var client) ? client : null;
        }
    }
}
