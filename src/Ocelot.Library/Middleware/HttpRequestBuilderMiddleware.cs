using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Library.Infrastructure.Repository;
using Ocelot.Library.Infrastructure.Requester;
using Ocelot.Library.Infrastructure.Responder;

namespace Ocelot.Library.Middleware
{
    using Infrastructure.RequestBuilder;

    public class HttpRequestBuilderMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHttpResponder _responder;
        private readonly IScopedRequestDataRepository _scopedRequestDataRepository;
        private readonly IRequestBuilder _requestBuilder;

        public HttpRequestBuilderMiddleware(RequestDelegate next, 
            IHttpResponder responder,
            IScopedRequestDataRepository scopedRequestDataRepository, 
            IRequestBuilder requestBuilder)
        {
            _next = next;
            _responder = responder;
            _scopedRequestDataRepository = scopedRequestDataRepository;
            _requestBuilder = requestBuilder;
        }

        public async Task Invoke(HttpContext context)
        {
            var downstreamUrl = _scopedRequestDataRepository.Get<string>("DownstreamUrl");

            if (downstreamUrl.IsError)
            {
                await _responder.CreateNotFoundResponse(context);
                return;
            }

            var request = await _requestBuilder
              .Build(context.Request.Method, downstreamUrl.Data, context.Request.Body,
              context.Request.Headers, context.Request.Cookies, context.Request.QueryString.Value, context.Request.ContentType);

            _scopedRequestDataRepository.Add("Request", request);

            await _next.Invoke(context);
        }
    }
}