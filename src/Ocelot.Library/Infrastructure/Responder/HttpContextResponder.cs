using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Ocelot.Library.Infrastructure.Responder
{
    public class HttpContextResponder : IHttpResponder
    {
        public async Task<HttpContext> CreateSuccessResponse(HttpContext context, HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                context.Response.StatusCode = (int)response.StatusCode;
                return context;
            }
            await context.Response.WriteAsync(await response.Content.ReadAsStringAsync());
            return context;
        }

        public async Task<HttpContext> CreateNotFoundResponse(HttpContext context)
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            return context;
        }
    }
}