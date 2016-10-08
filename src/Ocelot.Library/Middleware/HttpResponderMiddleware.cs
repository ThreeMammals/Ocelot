using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Library.Infrastructure.Repository;
using Ocelot.Library.Infrastructure.Responder;

namespace Ocelot.Library.Middleware
{
    /// <summary>
    /// Terminating middleware that is responsible for returning a http response to the client
    /// </summary>
    public class HttpResponderMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHttpResponder _responder;
        private readonly IScopedRequestDataRepository _scopedRequestDataRepository;

        public HttpResponderMiddleware(RequestDelegate next, 
            IHttpResponder responder,
            IScopedRequestDataRepository scopedRequestDataRepository)
        {
            _next = next;
            _responder = responder;
            _scopedRequestDataRepository = scopedRequestDataRepository;
        }

        public async Task Invoke(HttpContext context)
        {
            var response = _scopedRequestDataRepository.Get<HttpResponseMessage>("Response");

            await _responder.CreateResponse(context, response.Data);
        }
    }
}