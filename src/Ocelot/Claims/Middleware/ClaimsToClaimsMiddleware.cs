namespace Ocelot.Claims.Middleware
{
    using Ocelot.Infrastructure.RequestData;
    using Microsoft.AspNetCore.Http;
    using Ocelot.Logging;
    using Ocelot.Middleware;
    using System.Linq;
    using System.Threading.Tasks;

    public class ClaimsToClaimsMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAddClaimsToRequest _addClaimsToRequest;

        public ClaimsToClaimsMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IAddClaimsToRequest addClaimsToRequest,
            IRequestScopedDataRepository repo)
                : base(loggerFactory.CreateLogger<ClaimsToClaimsMiddleware>(), repo)
        {
            _next = next;
            _addClaimsToRequest = addClaimsToRequest;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (DownstreamContext.Data.DownstreamReRoute.ClaimsToClaims.Any())
            {
                Logger.LogDebug("this route has instructions to convert claims to other claims");

                var result = _addClaimsToRequest.SetClaimsOnContext(DownstreamContext.Data.DownstreamReRoute.ClaimsToClaims, httpContext);

                if (result.IsError)
                {
                    Logger.LogDebug("error converting claims to other claims, setting pipeline error");

                    SetPipelineError(httpContext, result.Errors);
                    return;
                }
            }

            await _next.Invoke(httpContext);
        }
    }
}
