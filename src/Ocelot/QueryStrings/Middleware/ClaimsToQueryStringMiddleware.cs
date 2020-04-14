namespace Ocelot.QueryStrings.Middleware
{
    using Ocelot.Logging;
    using Ocelot.Middleware;
    using System.Linq;
    using System.Threading.Tasks;
    using Infrastructure.RequestData;
    using Microsoft.AspNetCore.Http;

    public class ClaimsToQueryStringMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAddQueriesToRequest _addQueriesToRequest;

        public ClaimsToQueryStringMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IAddQueriesToRequest addQueriesToRequest,
            IRequestScopedDataRepository requestScopedDataRepository)
                : base(loggerFactory.CreateLogger<ClaimsToQueryStringMiddleware>(), requestScopedDataRepository)
        {
            _next = next;
            _addQueriesToRequest = addQueriesToRequest;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (DownstreamContext.Data.DownstreamReRoute.ClaimsToQueries.Any())
            {
                Logger.LogInformation($"{DownstreamContext.Data.DownstreamReRoute.DownstreamPathTemplate.Value} has instructions to convert claims to queries");

                var response = _addQueriesToRequest.SetQueriesOnDownstreamRequest(DownstreamContext.Data.DownstreamReRoute.ClaimsToQueries, httpContext.User.Claims, DownstreamContext.Data.DownstreamRequest);

                if (response.IsError)
                {
                    Logger.LogWarning("there was an error setting queries on context, setting pipeline error");

                    SetPipelineError(httpContext, response.Errors);
                    return;
                }
            }

            await _next.Invoke(httpContext);
        }
    }
}
