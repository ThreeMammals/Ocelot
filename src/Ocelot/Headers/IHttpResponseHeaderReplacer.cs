using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Responses;

namespace Ocelot.Headers
{
    public interface IHttpResponseHeaderReplacer
    {
        public Response Replace(HttpContext httpContext, List<HeaderFindAndReplace> fAndRs);
    }
}
