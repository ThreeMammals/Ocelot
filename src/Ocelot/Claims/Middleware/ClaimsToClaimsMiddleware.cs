namespace Ocelot.Claims.Middleware
{
    using Microsoft.AspNetCore.Http;
    using Ocelot.DownstreamRouteFinder.Middleware;
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
            IAddClaimsToRequest addClaimsToRequest)
                : base(loggerFactory.CreateLogger<ClaimsToClaimsMiddleware>())
        {
            _next = next;
            _addClaimsToRequest = addClaimsToRequest;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var downstreamReRoute = httpContext.Items.DownstreamReRoute();

            if (downstreamReRoute.ClaimsToClaims.Any())
            {
                Logger.LogDebug("this route has instructions to convert claims to other claims");

                var result = _addClaimsToRequest.SetClaimsOnContext(downstreamReRoute.ClaimsToClaims, httpContext);

                if (result.IsError)
                {
                    Logger.LogDebug("error converting claims to other claims, setting pipeline error");

                    httpContext.Items.UpsertErrors(result.Errors);
                    return;
                }
            }

            await _next.Invoke(httpContext);
        }
    }
}
