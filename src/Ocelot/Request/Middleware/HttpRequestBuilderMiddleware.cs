using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Middleware;
using Ocelot.Request.Builder;

namespace Ocelot.Request.Middleware
{
    public class HttpRequestBuilderMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IRequestBuilder _requestBuilder;

        public HttpRequestBuilderMiddleware(RequestDelegate next, 
            IRequestScopedDataRepository requestScopedDataRepository, 
            IRequestBuilder requestBuilder)
            :base(requestScopedDataRepository)
        {
            _next = next;
            _requestBuilder = requestBuilder;
        }

        public async Task Invoke(HttpContext context)
        {
            var request = await _requestBuilder
              .Build(context.Request.Method, DownstreamUrl, context.Request.Body,
              context.Request.Headers, context.Request.Cookies, context.Request.QueryString, context.Request.ContentType);

            if (request.IsError)
            {
                SetPipelineError(request.Errors);
                return;
            }

            SetUpstreamRequestForThisRequest(request.Data);

            await _next.Invoke(context);
        }
    }
}