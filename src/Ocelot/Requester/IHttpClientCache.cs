﻿using System;

using Ocelot.Configuration;

namespace Ocelot.Requester
{
    public interface IHttpClientCache
    {
        IHttpClient Get(DownstreamRoute key);

        void Set(DownstreamRoute key, IHttpClient handler, TimeSpan expirationTime);
    }
}
