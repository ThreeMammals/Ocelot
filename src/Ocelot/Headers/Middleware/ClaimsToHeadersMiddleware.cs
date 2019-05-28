using Ocelot.Logging;
using Ocelot.Middleware;
using System.Linq;
using System.Threading.Tasks;

namespace Ocelot.Headers.Middleware
{
    public class ClaimsToHeadersMiddleware : OcelotMiddleware
    {
        private readonly OcelotRequestDelegate _next;
        private readonly IAddHeadersToRequest _addHeadersToRequest;

        public ClaimsToHeadersMiddleware(OcelotRequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IAddHeadersToRequest addHeadersToRequest)
                : base(loggerFactory.CreateLogger<ClaimsToHeadersMiddleware>())
        {
            _next = next;
            _addHeadersToRequest = addHeadersToRequest;
        }

        public async Task Invoke(DownstreamContext context)
        {
            if (context.DownstreamReRoute.ClaimsToHeaders.Any())
            {
                Logger.LogInformation($"{context.DownstreamReRoute.DownstreamPathTemplate.Value} has instructions to convert claims to headers");

                var response = _addHeadersToRequest.SetHeadersOnDownstreamRequest(context.DownstreamReRoute.ClaimsToHeaders, context.HttpContext.User.Claims, context.DownstreamRequest);

                if (response.IsError)
                {
                    Logger.LogWarning("Error setting headers on context, setting pipeline error");

                    SetPipelineError(context, response.Errors);
                    return;
                }

                Logger.LogInformation("headers have been set on context");
            }

            await _next.Invoke(context);
        }
    }
}
