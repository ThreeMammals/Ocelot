using System.Collections.Generic;

using Ocelot.Configuration;

using Microsoft.AspNetCore.Http;

using Ocelot.Responses;

namespace Ocelot.Headers
{
    public interface IHttpResponseHeaderReplacer
    {
        public Response Replace(HttpContext httpContext, List<HeaderFindAndReplace> fAndRs);
    }
}
