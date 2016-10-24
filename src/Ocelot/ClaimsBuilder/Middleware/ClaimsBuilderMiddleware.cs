using Ocelot.Infrastructure.RequestData;

namespace Ocelot.ClaimsBuilder.Middleware
{
    using System.Linq;
    using System.Threading.Tasks;
    using DownstreamRouteFinder;
    using Microsoft.AspNetCore.Http;
    using Ocelot.Middleware;

    public class ClaimsBuilderMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAddClaimsToRequest _addClaimsToRequest;
        private readonly IRequestScopedDataRepository _requestScopedDataRepository;

        public ClaimsBuilderMiddleware(RequestDelegate next, 
            IRequestScopedDataRepository requestScopedDataRepository,
            IAddClaimsToRequest addClaimsToRequest) 
            : base(requestScopedDataRepository)
        {
            _next = next;
            _addClaimsToRequest = addClaimsToRequest;
            _requestScopedDataRepository = requestScopedDataRepository;
        }

        public async Task Invoke(HttpContext context)
        {
            var downstreamRoute = _requestScopedDataRepository.Get<DownstreamRoute>("DownstreamRoute");

            if (downstreamRoute.Data.ReRoute.ClaimsToClaims.Any())
            {
                var result = _addClaimsToRequest.SetClaimsOnContext(downstreamRoute.Data.ReRoute.ClaimsToClaims, context);

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
