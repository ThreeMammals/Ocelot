using Microsoft.AspNetCore.Http;
using Ocelot.Middleware;
using System.Threading.Tasks;

namespace Ocelot.Responder
{
    public interface IHttpResponder
    {
        Task SetResponseOnHttpContext(HttpContext context, DownstreamResponse response);

        void SetErrorResponseOnContext(HttpContext context, int statusCode);
    }
}
