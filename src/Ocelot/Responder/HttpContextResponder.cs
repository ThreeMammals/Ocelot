using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Ocelot.Headers;
using Ocelot.Responses;

namespace Ocelot.Responder
{
    /// <summary>
    /// Cannot unit test things in this class due to methods not being implemented
    /// on .net concretes used for testing
    /// </summary>
    public class HttpContextResponder : IHttpResponder
    {
        private readonly IRemoveHeaders _removeHeaders;

        public HttpContextResponder(IRemoveHeaders removeHeaders)
        {
            _removeHeaders = removeHeaders;
        }

        public async Task<Response> SetResponseOnHttpContext(HttpContext context, HttpResponseMessage response)
        {
            _removeHeaders.Remove(response.Headers);

            foreach (var httpResponseHeader in response.Headers)
            {
                context.Response.Headers.Add(httpResponseHeader.Key, new StringValues(httpResponseHeader.Value.ToArray()));
            }

            var content = await response.Content.ReadAsStreamAsync();

            context.Response.Headers.Add("Content-Length", new[] { content.Length.ToString() });

            context.Response.OnStarting(state =>
            {
                var httpContext = (HttpContext)state;

                httpContext.Response.StatusCode = (int)response.StatusCode;

                return Task.CompletedTask;

            }, context);

            using (var reader = new StreamReader(content))
            {
                var responseContent = reader.ReadToEnd();
                await context.Response.WriteAsync(responseContent);
            }

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