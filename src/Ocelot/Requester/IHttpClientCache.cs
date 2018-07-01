namespace Ocelot.Requester
{
    using System;

    public interface IHttpClientCache
    {
        IHttpClient Get(string id);
        void Set(string id, IHttpClient handler, TimeSpan expirationTime);
    }
}
