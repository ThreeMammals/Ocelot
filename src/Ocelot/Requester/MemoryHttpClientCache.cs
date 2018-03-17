using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Ocelot.Requester
{
    public class MemoryHttpClientCache : IHttpClientCache
    {
        private readonly ConcurrentDictionary<string, ConcurrentQueue<IHttpClient>> _httpClientsCache = new ConcurrentDictionary<string, ConcurrentQueue<IHttpClient>>();

        public void Set(string id, IHttpClient client, TimeSpan expirationTime)
        {
            ConcurrentQueue<IHttpClient> connectionQueue;
            if (_httpClientsCache.TryGetValue(id, out connectionQueue))
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

        public bool Exists(string id)
        {
            ConcurrentQueue<IHttpClient> connectionQueue;
            return _httpClientsCache.TryGetValue(id, out connectionQueue);
        }

        public IHttpClient Get(string id)
        {
            IHttpClient client= null;
            ConcurrentQueue<IHttpClient> connectionQueue;
            if (_httpClientsCache.TryGetValue(id, out connectionQueue))
            {
                connectionQueue.TryDequeue(out client);
            }

            return client;
        }

        public void Remove(string id)
        {
            ConcurrentQueue<IHttpClient> connectionQueue;
            _httpClientsCache.TryRemove(id, out connectionQueue);
        }        
    }

    public class MemoryHttpClientHandlerCache : IHttpClientHandlerCache
    {
        private readonly ConcurrentDictionary<string, ConcurrentQueue<HttpClientHandler>> _httpClientsCache = new ConcurrentDictionary<string, ConcurrentQueue<HttpClientHandler>>();

        public void Set(string id, HttpClientHandler client, TimeSpan expirationTime)
        {
            ConcurrentQueue<HttpClientHandler> connectionQueue;
            if (_httpClientsCache.TryGetValue(id, out connectionQueue))
            {
                connectionQueue.Enqueue(client);
            }
            else
            {
                connectionQueue = new ConcurrentQueue<HttpClientHandler>();
                connectionQueue.Enqueue(client);
                _httpClientsCache.TryAdd(id, connectionQueue);
            }
        }

        public bool Exists(string id)
        {
            ConcurrentQueue<HttpClientHandler> connectionQueue;
            return _httpClientsCache.TryGetValue(id, out connectionQueue);
        }

        public HttpClientHandler Get(string id)
        {
            HttpClientHandler client = null;
            ConcurrentQueue<HttpClientHandler> connectionQueue;
            if (_httpClientsCache.TryGetValue(id, out connectionQueue))
            {
                connectionQueue.TryDequeue(out client);
            }

            return client;
        }

        public void Remove(string id)
        {
            ConcurrentQueue<HttpClientHandler> connectionQueue;
            _httpClientsCache.TryRemove(id, out connectionQueue);
        }
    }
}
