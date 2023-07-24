using Microsoft.AspNetCore.Http;
using Ocelot.Middleware;

namespace Ocelot.Responder
{
    public interface IHttpResponder
    {
        Task SetResponseOnHttpContext(HttpContext context, DownstreamResponse response);

        void SetErrorResponseOnContext(HttpContext context, int statusCode);

        Task SetErrorResponseOnContext(HttpContext context, DownstreamResponse response);
    }
}
