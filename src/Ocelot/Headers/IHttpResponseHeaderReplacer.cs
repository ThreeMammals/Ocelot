using System.Collections.Generic;
using System.Net.Http;
using Ocelot.Configuration;
using Ocelot.Responses;

namespace Ocelot.Headers
{
    public interface IHttpResponseHeaderReplacer
    {
        Response Replace(HttpResponseMessage response, List<HeaderFindAndReplace> fAndRs, HttpRequestMessage httpRequestMessage);
    }
}