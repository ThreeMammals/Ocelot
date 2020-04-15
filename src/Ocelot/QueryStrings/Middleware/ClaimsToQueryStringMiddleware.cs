namespace Ocelot.QueryStrings.Middleware
{
    using Ocelot.Logging;
    using Ocelot.Middleware;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    public class ClaimsToQueryStringMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAddQueriesToRequest _addQueriesToRequest;

        public ClaimsToQueryStringMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IAddQueriesToRequest addQueriesToRequest)
                : base(loggerFactory.CreateLogger<ClaimsToQueryStringMiddleware>())
        {
            _next = next;
            _addQueriesToRequest = addQueriesToRequest;
        }

        public async Task Invoke(HttpContext httpContext, IDownstreamContext downstreamContext)
        {
            if (downstreamContext.DownstreamReRoute.ClaimsToQueries.Any())
            {
                Logger.LogInformation($"{downstreamContext.DownstreamReRoute.DownstreamPathTemplate.Value} has instructions to convert claims to queries");

                var response = _addQueriesToRequest.SetQueriesOnDownstreamRequest(downstreamContext.DownstreamReRoute.ClaimsToQueries, httpContext.User.Claims, downstreamContext.DownstreamRequest);

                if (response.IsError)
                {
                    Logger.LogWarning("there was an error setting queries on context, setting pipeline error");

                    SetPipelineError(downstreamContext, response.Errors);
                    return;
                }
            }

            await _next.Invoke(httpContext);
        }
    }
}
