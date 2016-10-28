using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Middleware;
using Ocelot.RequestBuilder.Builder;

namespace Ocelot.RequestBuilder.Middleware
{
    public class HttpRequestBuilderMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IRequestScopedDataRepository _requestScopedDataRepository;
        private readonly IRequestBuilder _requestBuilder;

        public HttpRequestBuilderMiddleware(RequestDelegate next, 
            IRequestScopedDataRepository requestScopedDataRepository, 
            IRequestBuilder requestBuilder)
            :base(requestScopedDataRepository)
        {
            _next = next;
            _requestScopedDataRepository = requestScopedDataRepository;
            _requestBuilder = requestBuilder;
        }

        public async Task Invoke(HttpContext context)
        {
            var downstreamUrl = _requestScopedDataRepository.Get<string>("DownstreamUrl");

            if (downstreamUrl.IsError)
            {
                SetPipelineError(downstreamUrl.Errors);
                return;
            }

            var request = await _requestBuilder
              .Build(context.Request.Method, downstreamUrl.Data, context.Request.Body,
              context.Request.Headers, context.Request.Cookies, context.Request.QueryString, context.Request.ContentType);

            if (request.IsError)
            {
                SetPipelineError(request.Errors);
                return;
            }

            _requestScopedDataRepository.Add("Request", request.Data);

            await _next.Invoke(context);
        }
    }
}