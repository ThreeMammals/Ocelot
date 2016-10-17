namespace Ocelot.Library.Middleware
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Repository;
    using RequestBuilder;

    public class HttpRequestBuilderMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IScopedRequestDataRepository _scopedRequestDataRepository;
        private readonly IRequestBuilder _requestBuilder;

        public HttpRequestBuilderMiddleware(RequestDelegate next, 
            IScopedRequestDataRepository scopedRequestDataRepository, 
            IRequestBuilder requestBuilder)
            :base(scopedRequestDataRepository)
        {
            _next = next;
            _scopedRequestDataRepository = scopedRequestDataRepository;
            _requestBuilder = requestBuilder;
        }

        public async Task Invoke(HttpContext context)
        {
            var downstreamUrl = _scopedRequestDataRepository.Get<string>("DownstreamUrl");

            if (downstreamUrl.IsError)
            {
                SetPipelineError(downstreamUrl.Errors);
                return;
            }

            var request = await _requestBuilder
              .Build(context.Request.Method, downstreamUrl.Data, context.Request.Body,
              context.Request.Headers, context.Request.Cookies, context.Request.QueryString.Value, context.Request.ContentType);

            if (request.IsError)
            {
                SetPipelineError(request.Errors);
                return;
            }

            _scopedRequestDataRepository.Add("Request", request.Data);

            await _next.Invoke(context);
        }
    }
}