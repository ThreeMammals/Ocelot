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

        public HttpHeadersTransformationMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IRequestScopedDataRepository requestScopedDataRepository,
            IHttpContextRequestHeaderReplacer preReplacer) 
            : base(requestScopedDataRepository)
        {
            _next = next;
            _preReplacer = preReplacer;
            _logger = loggerFactory.CreateLogger<HttpHeadersTransformationMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            var fAndRs = this.DownstreamRoute.ReRoute.UpstreamHeadersFindAndReplace;

            _preReplacer.Replace(context, fAndRs);

            await _next.Invoke(context);

            //foreach header find and replace after downstream request
            //maek the change
            //use this object
            //this.HttpResponseMessage
        }
    }
}