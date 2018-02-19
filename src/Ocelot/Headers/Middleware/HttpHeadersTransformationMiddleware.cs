using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.DownstreamRouteFinder.Middleware;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;

namespace Ocelot.Headers.Middleware
{
    public class HttpHeadersTransformationMiddleware : OcelotMiddlewareV2
    {
        private readonly OcelotRequestDelegate _next;
        private readonly IOcelotLogger _logger;
        private readonly IHttpContextRequestHeaderReplacer _preReplacer;
        private readonly IHttpResponseHeaderReplacer _postReplacer;

        public HttpHeadersTransformationMiddleware(OcelotRequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IHttpContextRequestHeaderReplacer preReplacer,
            IHttpResponseHeaderReplacer postReplacer) 
        {
            _next = next;
            _postReplacer = postReplacer;
            _preReplacer = preReplacer;
            _logger = loggerFactory.CreateLogger<HttpHeadersTransformationMiddleware>();
        }

        public async Task Invoke(DownstreamContext context)
        {
            var preFAndRs = context.DownstreamReRoute.UpstreamHeadersFindAndReplace;

            //todo - this should be on httprequestmessage not httpcontext?
            _preReplacer.Replace(context.HttpContext, preFAndRs);

            await _next.Invoke(context);

            var postFAndRs = context.DownstreamReRoute.DownstreamHeadersFindAndReplace;

            _postReplacer.Replace(context.DownstreamResponse, postFAndRs, context.DownstreamRequest);
        }
    }
}
