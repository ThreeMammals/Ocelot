namespace Ocelot.Library.Responder
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    public interface IHttpResponder
    {
        Task<HttpContext> CreateResponse(HttpContext context, HttpResponseMessage response);
        Task<HttpContext> CreateErrorResponse(HttpContext context, int statusCode);
    }
}
