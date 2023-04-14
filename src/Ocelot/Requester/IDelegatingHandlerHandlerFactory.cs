namespace Ocelot.Requester
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;

    using Configuration;
    using Responses;

    public interface IDelegatingHandlerHandlerFactory
    {
        Response<List<Func<DelegatingHandler>>> Get(DownstreamRoute downstreamRoute);
    }
}
