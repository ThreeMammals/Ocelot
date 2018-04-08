using System;

namespace Ocelot.Requester
{
    public interface IHttpClientCache
    {
        bool Exists(string id);
        IHttpClient Get(string id);
        void Remove(string id);
        void Set(string id, IHttpClient handler, TimeSpan expirationTime);
    }
}
