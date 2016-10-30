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
            var buildResult = await _requestBuilder
                .Build(context.Request.Method, DownstreamUrl, context.Request.Body,
                    context.Request.Headers, context.Request.Cookies, context.Request.QueryString,
                    context.Request.ContentType, new RequestId.RequestId(DownstreamRoute?.ReRoute?.RequestIdKey, context.TraceIdentifier));

            if (buildResult.IsError)
            {
                SetPipelineError(buildResult.Errors);
                return;
            }

            SetUpstreamRequestForThisRequest(buildResult.Data);

            await _next.Invoke(context);
        }
    }
}