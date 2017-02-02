using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Responses;

namespace Ocelot.Responder
{
    public interface IHttpResponder
    {
        Task<Response> SetResponseOnHttpContext(HttpContext context, HttpResponseMessage response);
        Task<Response> SetErrorResponseOnContext(HttpContext context, int statusCode);
    }
}
