namespace Ocelot.Headers.Middleware
{
    using Ocelot.Infrastructure.RequestData;
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
            IAddHeadersToRequest addHeadersToRequest,
            IRequestScopedDataRepository repo)
                : base(loggerFactory.CreateLogger<ClaimsToHeadersMiddleware>(), repo)
        {
            _next = next;
            _addHeadersToRequest = addHeadersToRequest;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (DownstreamContext.Data.DownstreamReRoute.ClaimsToHeaders.Any())
            {
                Logger.LogInformation($"{DownstreamContext.Data.DownstreamReRoute.DownstreamPathTemplate.Value} has instructions to convert claims to headers");

                var response = _addHeadersToRequest.SetHeadersOnDownstreamRequest(DownstreamContext.Data.DownstreamReRoute.ClaimsToHeaders, httpContext.User.Claims, DownstreamContext.Data.DownstreamRequest);

                if (response.IsError)
                {
                    Logger.LogWarning("Error setting headers on context, setting pipeline error");

                    SetPipelineError(httpContext, response.Errors);
                    return;
                }

                Logger.LogInformation("headers have been set on context");
            }

            await _next.Invoke(httpContext);
        }
    }
}
