namespace Ocelot.ClaimsBuilder.Middleware
{
    using System.Linq;
    using System.Threading.Tasks;
    using DownstreamRouteFinder;
    using Microsoft.AspNetCore.Http;
    using Ocelot.Middleware;
    using ScopedData;

    public class ClaimsBuilderMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAddClaimsToRequest _addClaimsToRequest;
        private readonly IScopedRequestDataRepository _scopedRequestDataRepository;

        public ClaimsBuilderMiddleware(RequestDelegate next, 
            IScopedRequestDataRepository scopedRequestDataRepository,
            IAddClaimsToRequest addClaimsToRequest) 
            : base(scopedRequestDataRepository)
        {
            _next = next;
            _addClaimsToRequest = addClaimsToRequest;
            _scopedRequestDataRepository = scopedRequestDataRepository;
        }

        public async Task Invoke(HttpContext context)
        {
            var downstreamRoute = _scopedRequestDataRepository.Get<DownstreamRoute>("DownstreamRoute");

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
