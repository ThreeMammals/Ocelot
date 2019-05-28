namespace Ocelot.Requester
{
    using Configuration;
    using System;

    public interface IHttpClientCache
    {
        IHttpClient Get(DownstreamReRoute key);

        void Set(DownstreamReRoute key, IHttpClient handler, TimeSpan expirationTime);
    }
}
