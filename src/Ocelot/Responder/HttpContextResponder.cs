using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Responses;

namespace Ocelot.Responder
{
    /// <summary>
    /// Cannot unit test things in this class due to methods not being implemented
    /// on .net concretes used for testing
    /// </summary>
    public class HttpContextResponder : IHttpResponder
    {
        public async Task<Response> SetResponseOnHttpContext(HttpContext context, HttpResponseMessage response)
        {
            context.Response.OnStarting(x =>
            {
                context.Response.StatusCode = (int)response.StatusCode;
                return Task.CompletedTask;
            }, context);

            await context.Response.WriteAsync(await response.Content.ReadAsStringAsync());
            return new OkResponse();       
        }

        public async Task<Response> SetErrorResponseOnContext(HttpContext context, int statusCode)
        {
            context.Response.OnStarting(x =>
            {
                context.Response.StatusCode = statusCode;
                return Task.CompletedTask;
            }, context);
            return new OkResponse();
        }
    }
}