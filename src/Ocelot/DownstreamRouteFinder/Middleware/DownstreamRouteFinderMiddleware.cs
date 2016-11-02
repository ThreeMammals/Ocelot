using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.DownstreamRouteFinder.Finder;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Middleware;

namespace Ocelot.DownstreamRouteFinder.Middleware
{
    public class DownstreamRouteFinderMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDownstreamRouteFinder _downstreamRouteFinder;

        public DownstreamRouteFinderMiddleware(RequestDelegate next, 
            IDownstreamRouteFinder downstreamRouteFinder, 
            IRequestScopedDataRepository requestScopedDataRepository)
            :base(requestScopedDataRepository)
        {
            _next = next;
            _downstreamRouteFinder = downstreamRouteFinder;
        }

        public async Task Invoke(HttpContext context)
        {
            var upstreamUrlPath = context.Request.Path.ToString();

            var downstreamRoute = _downstreamRouteFinder.FindDownstreamRoute(upstreamUrlPath, context.Request.Method);

            if (downstreamRoute.IsError)
            {
                SetPipelineError(downstreamRoute.Errors);
                return;
            }

            SetDownstreamRouteForThisRequest(downstreamRoute.Data);

            await _next.Invoke(context);
        }
    }
}