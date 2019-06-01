using Ocelot.Logging;
using Ocelot.Middleware;
using System.Threading.Tasks;

namespace Ocelot.Headers.Middleware
{
    public class HttpHeadersTransformationMiddleware : OcelotMiddleware
    {
        private readonly OcelotRequestDelegate _next;
        private readonly IHttpContextRequestHeaderReplacer _preReplacer;
        private readonly IHttpResponseHeaderReplacer _postReplacer;
        private readonly IAddHeadersToResponse _addHeadersToResponse;
        private readonly IAddHeadersToRequest _addHeadersToRequest;

        public HttpHeadersTransformationMiddleware(OcelotRequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IHttpContextRequestHeaderReplacer preReplacer,
            IHttpResponseHeaderReplacer postReplacer,
            IAddHeadersToResponse addHeadersToResponse,
            IAddHeadersToRequest addHeadersToRequest)
                : base(loggerFactory.CreateLogger<HttpHeadersTransformationMiddleware>())
        {
            _addHeadersToResponse = addHeadersToResponse;
            _addHeadersToRequest = addHeadersToRequest;
            _next = next;
            _postReplacer = postReplacer;
            _preReplacer = preReplacer;
        }

        public async Task Invoke(DownstreamContext context)
        {
            var preFAndRs = context.DownstreamReRoute.UpstreamHeadersFindAndReplace;

            //todo - this should be on httprequestmessage not httpcontext?
            _preReplacer.Replace(context.HttpContext, preFAndRs);

            _addHeadersToRequest.SetHeadersOnDownstreamRequest(context.DownstreamReRoute.AddHeadersToUpstream, context.HttpContext);

            await _next.Invoke(context);

            if (context.IsError)
            {
                return;
            }

            var postFAndRs = context.DownstreamReRoute.DownstreamHeadersFindAndReplace;

            _postReplacer.Replace(context, postFAndRs);

            _addHeadersToResponse.Add(context.DownstreamReRoute.AddHeadersToDownstream, context.DownstreamResponse);
        }
    }
}
