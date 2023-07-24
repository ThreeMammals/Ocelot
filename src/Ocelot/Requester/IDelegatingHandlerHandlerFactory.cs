using Ocelot.Configuration;
using Ocelot.Responses;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Ocelot.Requester
{
    public interface IDelegatingHandlerHandlerFactory
    {
        Response<List<Func<DelegatingHandler>>> Get(DownstreamRoute downstreamRoute);
    }
}
