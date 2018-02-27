using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.DownstreamRouteFinder.Middleware;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;

namespace Ocelot.Headers.Middleware
{
    public class HttpRequestHeadersBuilderMiddleware : OcelotMiddleware
    {
        private readonly OcelotRequestDelegate _next;
        private readonly IAddHeadersToRequest _addHeadersToRequest;
        private readonly IOcelotLogger _logger;

        public HttpRequestHeadersBuilderMiddleware(OcelotRequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IAddHeadersToRequest addHeadersToRequest) 
        {
            _next = next;
            _addHeadersToRequest = addHeadersToRequest;
            _logger = loggerFactory.CreateLogger<HttpRequestHeadersBuilderMiddleware>();
        }

        public async Task Invoke(DownstreamContext context)
        {
            if (context.DownstreamReRoute.ClaimsToHeaders.Any())
            {
                _logger.LogDebug($"{ context.DownstreamReRoute.DownstreamPathTemplate.Value} has instructions to convert claims to headers");

                var response = _addHeadersToRequest.SetHeadersOnDownstreamRequest(context.DownstreamReRoute.ClaimsToHeaders, context.HttpContext.User.Claims, context.DownstreamRequest);

                if (response.IsError)
                {
                    _logger.LogDebug("Error setting headers on context, setting pipeline error");

                    SetPipelineError(context, response.Errors);
                    return;
                }

                _logger.LogDebug("headers have been set on context");
            }

            await _next.Invoke(context);
        }
    }
}
