namespace Ocelot.Headers
{
    using System.Collections.Generic;

    using Configuration;

    using Microsoft.AspNetCore.Http;

    using Responses;

    public interface IHttpResponseHeaderReplacer
    {
        public Response Replace(HttpContext httpContext, List<HeaderFindAndReplace> fAndRs);
    }
}
