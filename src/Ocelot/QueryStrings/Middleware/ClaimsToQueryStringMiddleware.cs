namespace Ocelot.QueryStrings.Middleware
{
    using Ocelot.Logging;
    using Ocelot.Middleware;
    using System.Linq;
    using System.Threading.Tasks;

    public class ClaimsToQueryStringMiddleware : OcelotMiddleware
    {
        private readonly OcelotRequestDelegate _next;
        private readonly IAddQueriesToRequest _addQueriesToRequest;

        public ClaimsToQueryStringMiddleware(OcelotRequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IAddQueriesToRequest addQueriesToRequest)
                : base(loggerFactory.CreateLogger<ClaimsToQueryStringMiddleware>())
        {
            _next = next;
            _addQueriesToRequest = addQueriesToRequest;
        }

        public async Task Invoke(DownstreamContext context)
        {
            if (context.DownstreamReRoute.ClaimsToQueries.Any())
            {
                Logger.LogInformation($"{context.DownstreamReRoute.DownstreamPathTemplate.Value} has instructions to convert claims to queries");

                var response = _addQueriesToRequest.SetQueriesOnDownstreamRequest(context.DownstreamReRoute.ClaimsToQueries, context.HttpContext.User.Claims, context.DownstreamRequest);

                if (response.IsError)
                {
                    Logger.LogWarning("there was an error setting queries on context, setting pipeline error");

                    SetPipelineError(context, response.Errors);
                    return;
                }
            }

            await _next.Invoke(context);
        }
    }
}
