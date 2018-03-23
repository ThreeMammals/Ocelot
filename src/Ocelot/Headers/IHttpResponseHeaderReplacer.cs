using System.Collections.Generic;
using System.Net.Http;
using Ocelot.Configuration;
using Ocelot.Request.Middleware;
using Ocelot.Responses;

namespace Ocelot.Headers
{
    public interface IHttpResponseHeaderReplacer
    {
        Response Replace(HttpResponseMessage response, List<HeaderFindAndReplace> fAndRs, DownstreamRequest httpRequestMessage);
    }
}