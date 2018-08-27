namespace Ocelot.Requester
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using Ocelot.Configuration;
    using Ocelot.Responses;

    public interface IDelegatingHandlerHandlerFactory
    {
        Response<List<Func<DelegatingHandler>>> Get(DownstreamReRoute downstreamReRoute);
    }
}
