namespace Ocelot.Headers
{
    using System.Collections.Generic;

    using Microsoft.AspNetCore.Http;

    using Configuration;
    using Responses;

    public interface IHttpResponseHeaderReplacer
    {
        public Response Replace(HttpContext httpContext, List<HeaderFindAndReplace> fAndRs);
    }
}
