using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Middleware;

namespace Ocelot.QueryStrings.Middleware
{
    public class QueryStringBuilderMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAddQueriesToRequest _addQueriesToRequest;

        public QueryStringBuilderMiddleware(RequestDelegate next, 
            IRequestScopedDataRepository requestScopedDataRepository,
            IAddQueriesToRequest addQueriesToRequest) 
            : base(requestScopedDataRepository)
        {
            _next = next;
            _addQueriesToRequest = addQueriesToRequest;
        }

        public async Task Invoke(HttpContext context)
        {
            if (DownstreamRoute.ReRoute.ClaimsToQueries.Any())
            {
                _addQueriesToRequest.SetQueriesOnContext(DownstreamRoute.ReRoute.ClaimsToQueries, context);
            }
            
            await _next.Invoke(context);
        }
    }
}
