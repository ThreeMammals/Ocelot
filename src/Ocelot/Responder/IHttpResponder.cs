namespace Ocelot.Responder
{
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Http;

    using Ocelot.Middleware;

    public interface IHttpResponder
    {
        Task SetResponseOnHttpContext(HttpContext context, DownstreamResponse response);

        void SetErrorResponseOnContext(HttpContext context, int statusCode);

        Task SetErrorResponseOnContext(HttpContext context, DownstreamResponse response);
    }
}
