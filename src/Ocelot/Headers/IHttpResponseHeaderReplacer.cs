namespace Ocelot.Headers
{
    using Ocelot.Configuration;
    using Ocelot.Middleware;
    using Ocelot.Responses;
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Http;

    public interface IHttpResponseHeaderReplacer
    {
        Response Replace(IDownstreamContext downstreamContext, HttpContext httpContext, List<HeaderFindAndReplace> fAndRs);
    }
}
