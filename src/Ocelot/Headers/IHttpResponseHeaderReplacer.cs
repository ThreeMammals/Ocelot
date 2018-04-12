using System.Collections.Generic;
using Ocelot.Configuration;
using Ocelot.Middleware;
using Ocelot.Middleware.Multiplexer;
using Ocelot.Request.Middleware;
using Ocelot.Responses;

namespace Ocelot.Headers
{
    public interface IHttpResponseHeaderReplacer
    {
        Response Replace(DownstreamResponse response, List<HeaderFindAndReplace> fAndRs, DownstreamRequest httpRequestMessage);
    }
}
