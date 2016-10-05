using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Library.Infrastructure.DownstreamRouteFinder;
using Ocelot.Library.Infrastructure.Responder;
using Ocelot.Library.Infrastructure.Services;

namespace Ocelot.Library.Middleware
{
    public class DownstreamRouteFinderMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDownstreamRouteFinder _downstreamRouteFinder;
        private readonly IHttpResponder _responder;
        private readonly IRequestDataService _requestDataService;

        public DownstreamRouteFinderMiddleware(RequestDelegate next, 
            IDownstreamRouteFinder downstreamRouteFinder, 
            IHttpResponder responder,
            IRequestDataService requestDataService)
        {
            _next = next;
            _downstreamRouteFinder = downstreamRouteFinder;
            _responder = responder;
            _requestDataService = requestDataService;
        }

        public async Task Invoke(HttpContext context)
        {   
            var upstreamUrlPath = context.Request.Path.ToString();

            var downstreamRoute = _downstreamRouteFinder.FindDownstreamRoute(upstreamUrlPath, context.Request.Method);

            if (downstreamRoute.IsError)
            {
                await _responder.CreateNotFoundResponse(context);
                return;
            }

            _requestDataService.Add("DownstreamRoute", downstreamRoute.Data);
            
            await _next.Invoke(context);
        }
    }
}