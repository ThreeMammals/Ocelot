using System;
using System.Collections.Generic;
using System.Net.Http;
using Ocelot.Configuration;
using Ocelot.Responses;

namespace Ocelot.Requester
{
    public interface IDelegatingHandlerHandlerFactory
    {
        Response<List<Func<DelegatingHandler>>> Get(DownstreamReRoute request);
    }
}
