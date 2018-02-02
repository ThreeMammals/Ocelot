using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;

namespace Ocelot.Headers.Middleware
{
    public class HttpHeadersTransformationMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IOcelotLogger _logger;
        private readonly IHttpContextRequestHeaderReplacer _preReplacer;
        private readonly IHttpResponseHeaderReplacer _postReplacer;

        public HttpHeadersTransformationMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IRequestScopedDataRepository requestScopedDataRepository,
            IHttpContextRequestHeaderReplacer preReplacer,
            IHttpResponseHeaderReplacer postReplacer) 
            : base(requestScopedDataRepository)
        {
            _next = next;
            _postReplacer = postReplacer;
            _preReplacer = preReplacer;
            _logger = loggerFactory.CreateLogger<HttpHeadersTransformationMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            var preFAndRs = this.DownstreamRoute.ReRoute.UpstreamHeadersFindAndReplace;

            _preReplacer.Replace(context, preFAndRs);

            await _next.Invoke(context);

            var postFAndRs = this.DownstreamRoute.ReRoute.DownstreamHeadersFindAndReplace;

            _postReplacer.Replace(HttpResponseMessage, postFAndRs, DownstreamRequest);
        }
    }
}