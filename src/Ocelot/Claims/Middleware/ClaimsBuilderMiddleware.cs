using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.DownstreamRouteFinder.Middleware;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;

namespace Ocelot.Claims.Middleware
{
    public class ClaimsBuilderMiddleware : OcelotMiddleware
    {
        private readonly OcelotRequestDelegate _next;
        private readonly IAddClaimsToRequest _addClaimsToRequest;
        private readonly IOcelotLogger _logger;

        public ClaimsBuilderMiddleware(OcelotRequestDelegate next, 
            IOcelotLoggerFactory loggerFactory,
            IAddClaimsToRequest addClaimsToRequest) 
        {
            _next = next;
            _addClaimsToRequest = addClaimsToRequest;
            _logger = loggerFactory.CreateLogger<ClaimsBuilderMiddleware>();
        }

        public async Task Invoke(DownstreamContext context)
        {
            if (context.DownstreamReRoute.ClaimsToClaims.Any())
            {
                _logger.LogDebug("this route has instructions to convert claims to other claims");

                var result = _addClaimsToRequest.SetClaimsOnContext(context.DownstreamReRoute.ClaimsToClaims, context.HttpContext);

                if (result.IsError)
                {
                    _logger.LogDebug("error converting claims to other claims, setting pipeline error");

                    SetPipelineError(context, result.Errors);
                    return;
                }
            }
            await _next.Invoke(context);
        }
    }
}
