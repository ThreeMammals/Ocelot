using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Ocelot.Responder
{
    public interface IHttpResponder
    {
        Task SetResponseOnHttpContext(HttpContext context, HttpResponseMessage response);
        void SetErrorResponseOnContext(HttpContext context, int statusCode);
    }
}
