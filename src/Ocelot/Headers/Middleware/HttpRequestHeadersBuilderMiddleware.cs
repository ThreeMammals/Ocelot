using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;

namespace Ocelot.Headers.Middleware
{
    public class HttpRequestHeadersBuilderMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAddHeadersToRequest _addHeadersToRequest;
        private readonly IOcelotLogger _logger;

        public HttpRequestHeadersBuilderMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IRequestScopedDataRepository requestScopedDataRepository,
            IAddHeadersToRequest addHeadersToRequest) 
            : base(requestScopedDataRepository)
        {
            _next = next;
            _addHeadersToRequest = addHeadersToRequest;
            _logger = loggerFactory.CreateLogger<HttpRequestHeadersBuilderMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            if (DownstreamRoute.ReRoute.ClaimsToHeaders.Any())
            {
                _logger.LogDebug($"{ DownstreamRoute.ReRoute.DownstreamPathTemplate.Value} has instructions to convert claims to headers");

                var response = _addHeadersToRequest.SetHeadersOnDownstreamRequest(DownstreamRoute.ReRoute.ClaimsToHeaders, context.User.Claims, DownstreamRequest);

                if (response.IsError)
                {
                    _logger.LogDebug("Error setting headers on context, setting pipeline error");

                    SetPipelineError(response.Errors);
                    return;
                }

                _logger.LogDebug("headers have been set on context");
            }

            await _next.Invoke(context);
        }
    }
}
