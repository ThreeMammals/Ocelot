namespace Ocelot.Requester
{
    using System;
    using System.Collections.Concurrent;

    public class MemoryHttpClientCache : IHttpClientCache
    {
        private readonly ConcurrentDictionary<string, IHttpClient> _httpClientsCache;

        public MemoryHttpClientCache()
        {
            _httpClientsCache = new ConcurrentDictionary<string, IHttpClient>();
        }

        public void Set(string key, IHttpClient client, TimeSpan expirationTime)
        {
            _httpClientsCache.AddOrUpdate(key, client, (k, oldValue) => client);
        }

        public IHttpClient Get(string key)
        {
            //todo handle error?
            return _httpClientsCache.TryGetValue(key, out var client) ? client : null;
        }      
    }
}
