namespace Ocelot.Headers.Middleware
{
    using Microsoft.AspNetCore.Http;
    using Ocelot.Logging;
    using Ocelot.Middleware;
    using System.Linq;
    using System.Threading.Tasks;

    public class ClaimsToHeadersMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAddHeadersToRequest _addHeadersToRequest;

        public ClaimsToHeadersMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IAddHeadersToRequest addHeadersToRequest)
                : base(loggerFactory.CreateLogger<ClaimsToHeadersMiddleware>())
        {
            _next = next;
            _addHeadersToRequest = addHeadersToRequest;
        }

        public async Task Invoke(HttpContext httpContext, IDownstreamContext downstreamContext)
        {
            var downstreamReRoute = Get(httpContext, downstreamContext);

            if (downstreamReRoute.ClaimsToHeaders.Any())
            {
                Logger.LogInformation($"{downstreamReRoute.DownstreamPathTemplate.Value} has instructions to convert claims to headers");

                var response = _addHeadersToRequest.SetHeadersOnDownstreamRequest(downstreamReRoute.ClaimsToHeaders, httpContext.User.Claims, downstreamContext.DownstreamRequest);

                if (response.IsError)
                {
                    Logger.LogWarning("Error setting headers on context, setting pipeline error");

                    SetPipelineError(downstreamContext, response.Errors);
                    return;
                }

                Logger.LogInformation("headers have been set on context");
            }

            await _next.Invoke(httpContext);
        }
    }
}
