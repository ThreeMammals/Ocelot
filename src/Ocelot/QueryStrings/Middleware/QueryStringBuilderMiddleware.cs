using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;

namespace Ocelot.QueryStrings.Middleware
{
    public class QueryStringBuilderMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAddQueriesToRequest _addQueriesToRequest;
        private readonly IOcelotLogger _logger;

        public QueryStringBuilderMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IRequestScopedDataRepository requestScopedDataRepository,
            IAddQueriesToRequest addQueriesToRequest) 
            : base(requestScopedDataRepository)
        {
            _next = next;
            _addQueriesToRequest = addQueriesToRequest;
            _logger = loggerFactory.CreateLogger<QueryStringBuilderMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            if (DownstreamRoute.ReRoute.ClaimsToQueries.Any())
            {
                _logger.LogDebug($"{DownstreamRoute.ReRoute.DownstreamPathTemplate.Value} has instructions to convert claims to queries");

                var response = _addQueriesToRequest.SetQueriesOnDownstreamRequest(DownstreamRoute.ReRoute.ClaimsToQueries, context.User.Claims, DownstreamRequest);

                if (response.IsError)
                {
                    _logger.LogDebug("there was an error setting queries on context, setting pipeline error");

                    SetPipelineError(response.Errors);
                    return;
                }
            }

            await _next.Invoke(context);
        }
    }
}
