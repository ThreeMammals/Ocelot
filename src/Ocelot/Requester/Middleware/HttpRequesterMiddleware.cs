using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Middleware;
using Ocelot.Responder;

namespace Ocelot.Requester.Middleware
{
    public class HttpRequesterMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHttpRequester _requester;
        private readonly IHttpResponder _responder;

        public HttpRequesterMiddleware(RequestDelegate next, 
            IHttpRequester requester, 
            IRequestScopedDataRepository requestScopedDataRepository, 
            IHttpResponder responder)
            :base(requestScopedDataRepository)
        {
            _next = next;
            _requester = requester;
            _responder = responder;
        }

        public async Task Invoke(HttpContext context)
        {

            var response = await _requester.GetResponse(Request);

            if (response.IsError)
            {
                SetPipelineError(response.Errors);
                return;
            }

            var setResponse = await _responder.SetResponseOnHttpContext(context, response.Data);

            if (setResponse.IsError)
            {
                SetPipelineError(response.Errors);
            }
        }
    }
}