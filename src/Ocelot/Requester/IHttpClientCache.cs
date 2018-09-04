namespace Ocelot.Requester
{
    using System;
    using Configuration;

    public interface IHttpClientCache
    {
        IHttpClient Get(DownstreamReRoute key);
        void Set(DownstreamReRoute key, IHttpClient handler, TimeSpan expirationTime);
    }
}
