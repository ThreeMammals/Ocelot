using System;
using System.Net.Http;

namespace Ocelot.Requester
{
    public interface IHttpClientHandlerCache
    {
        bool Exists(string id);
        HttpClientHandler Get(string id);
        void Remove(string id);
        void Set(string id, HttpClientHandler client, TimeSpan expirationTime);
    }
}
