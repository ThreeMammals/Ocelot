namespace Ocelot.Requester
{
    using System;

    public interface IHttpClientCache
    {
        IHttpClient Get(string key);
        void Set(string key, IHttpClient handler, TimeSpan expirationTime);
    }
}
