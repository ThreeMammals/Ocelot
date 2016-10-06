using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Library.Infrastructure.Repository;
using Ocelot.Library.Infrastructure.Requester;
using Ocelot.Library.Infrastructure.Responder;

namespace Ocelot.Library.Middleware
{
    using Infrastructure.RequestBuilder;

    public class HttpRequesterMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHttpRequester _requester;
        private readonly IHttpResponder _responder;
        private readonly IScopedRequestDataRepository _scopedRequestDataRepository;
        private readonly IRequestBuilder _requestBuilder;

        public HttpRequesterMiddleware(RequestDelegate next, 
            IHttpRequester requester, 
            IHttpResponder responder,
            IScopedRequestDataRepository scopedRequestDataRepository, 
            IRequestBuilder requestBuilder)
        {
            _next = next;
            _requester = requester;
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

            var request = _requestBuilder
              .Build(context.Request.Method, downstreamUrl.Data, context.Request.Body,
              context.Request.Headers, context.Request.Cookies, context.Request.Query, context.Request.ContentType);

            var response = await _requester
                .GetResponse(request);

            await _responder.CreateResponse(context, response);

            await _next.Invoke(context);
        }
    }
}