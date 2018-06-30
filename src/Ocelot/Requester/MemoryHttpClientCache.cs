namespace Ocelot.Requester
{
    using System;
    using System.Collections.Concurrent;

    public class MemoryHttpClientCache : IHttpClientCache
    {
        private readonly ConcurrentDictionary<string, ConcurrentQueue<IHttpClient>> _httpClientsCache;

        public MemoryHttpClientCache()
        {
            _httpClientsCache = new ConcurrentDictionary<string, ConcurrentQueue<IHttpClient>>();
        }

        public void Set(string id, IHttpClient client, TimeSpan expirationTime)
        {
            if (_httpClientsCache.TryGetValue(id, out var connectionQueue))
            {
                connectionQueue.Enqueue(client);
            }
            else
            {
                connectionQueue = new ConcurrentQueue<IHttpClient>();
                connectionQueue.Enqueue(client);
                _httpClientsCache.TryAdd(id, connectionQueue);
            }
        }

        public IHttpClient Get(string id)
        {
            IHttpClient client= null;
            if (_httpClientsCache.TryGetValue(id, out var connectionQueue))
            {
                connectionQueue.TryDequeue(out client);
            }

            return client;
        }      
    }
}
