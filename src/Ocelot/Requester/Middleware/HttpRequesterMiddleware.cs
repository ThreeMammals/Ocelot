using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Middleware;

namespace Ocelot.Requester.Middleware
{
    public class HttpRequesterMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHttpRequester _requester;

        public HttpRequesterMiddleware(RequestDelegate next, 
            IHttpRequester requester, 
            IRequestScopedDataRepository requestScopedDataRepository)
            :base(requestScopedDataRepository)
        {
            _next = next;
            _requester = requester;
        }

        public async Task Invoke(HttpContext context)
        {
            var response = await _requester.GetResponse(Request);

            if (response.IsError)
            {
                SetPipelineError(response.Errors);
                return;
            }

            SetHttpResponseMessageThisRequest(response.Data);
        }
    }
}