namespace Ocelot.Headers.Middleware
{
    using Microsoft.AspNetCore.Http;
    using Ocelot.Logging;
    using Ocelot.Middleware;
    using System.Threading.Tasks;

    public class HttpHeadersTransformationMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHttpContextRequestHeaderReplacer _preReplacer;
        private readonly IHttpResponseHeaderReplacer _postReplacer;
        private readonly IAddHeadersToResponse _addHeadersToResponse;
        private readonly IAddHeadersToRequest _addHeadersToRequest;

        public HttpHeadersTransformationMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IHttpContextRequestHeaderReplacer preReplacer,
            IHttpResponseHeaderReplacer postReplacer,
            IAddHeadersToResponse addHeadersToResponse,
            IAddHeadersToRequest addHeadersToRequest
            )
                : base(loggerFactory.CreateLogger<HttpHeadersTransformationMiddleware>())
        {
            _addHeadersToResponse = addHeadersToResponse;
            _addHeadersToRequest = addHeadersToRequest;
            _next = next;
            _postReplacer = postReplacer;
            _preReplacer = preReplacer;
        }

        public async Task Invoke(HttpContext httpContext, IDownstreamContext downstreamContext)
        {
            var downstreamReRoute = Get(httpContext, downstreamContext);

            var preFAndRs = downstreamReRoute.UpstreamHeadersFindAndReplace;

            //todo - this should be on httprequestmessage not httpcontext?
            _preReplacer.Replace(httpContext, preFAndRs);

            _addHeadersToRequest.SetHeadersOnDownstreamRequest(downstreamReRoute.AddHeadersToUpstream, httpContext);

            await _next.Invoke(httpContext);

            // todo check errors is ok
            //todo put this check on the base class?
            if (downstreamContext.Errors.Count > 0)
            {
                return;
            }

            var postFAndRs = downstreamReRoute.DownstreamHeadersFindAndReplace;

            _postReplacer.Replace(downstreamContext, httpContext, postFAndRs);

            _addHeadersToResponse.Add(downstreamReRoute.AddHeadersToDownstream, downstreamContext.DownstreamResponse);
        }
    }
}
