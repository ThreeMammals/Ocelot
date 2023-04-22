namespace Ocelot.Requester
{
    using System;

    using Configuration;

    public interface IHttpClientCache
    {
        IHttpClient Get(DownstreamRoute key);

        void Set(DownstreamRoute key, IHttpClient handler, TimeSpan expirationTime);
    }
}
