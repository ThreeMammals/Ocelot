using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Ocelot.Responder
{
    public interface IHttpResponder
    {
        Task<HttpContext> CreateResponse(HttpContext context, HttpResponseMessage response);
        Task<HttpContext> CreateErrorResponse(HttpContext context, int statusCode);
    }
}
