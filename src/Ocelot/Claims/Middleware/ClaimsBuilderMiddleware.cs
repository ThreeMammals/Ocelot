using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Middleware;

namespace Ocelot.Claims.Middleware
{
    public class ClaimsBuilderMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAddClaimsToRequest _addClaimsToRequest;

        public ClaimsBuilderMiddleware(RequestDelegate next, 
            IRequestScopedDataRepository requestScopedDataRepository,
            IAddClaimsToRequest addClaimsToRequest) 
            : base(requestScopedDataRepository)
        {
            _next = next;
            _addClaimsToRequest = addClaimsToRequest;
        }

        public async Task Invoke(HttpContext context)
        {
            if (DownstreamRoute.ReRoute.ClaimsToClaims.Any())
            {
                var result = _addClaimsToRequest.SetClaimsOnContext(DownstreamRoute.ReRoute.ClaimsToClaims, context);

                if (result.IsError)
                {
                    SetPipelineError(result.Errors);
                    return;
                }
            }
            
            await _next.Invoke(context);
        }
    }
}
