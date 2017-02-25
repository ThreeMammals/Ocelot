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
            _logger.LogDebug("started calling headers builder middleware");

            if (DownstreamRoute.ReRoute.ClaimsToHeaders.Any())
            {
                _logger.LogDebug("this route has instructions to convert claims to headers");

                var response = _addHeadersToRequest.SetHeadersOnContext(DownstreamRoute.ReRoute.ClaimsToHeaders, context);

                if (response.IsError)
                {
                    _logger.LogDebug("there was an error setting headers on context, setting pipeline error");

                    SetPipelineError(response.Errors);
                    return;
                }

                _logger.LogDebug("headers have been set on context");
            }

            _logger.LogDebug("calling next middleware");

            await _next.Invoke(context);

            _logger.LogDebug("succesfully called next middleware");
        }
    }
}
