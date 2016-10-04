using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Library.Infrastructure.DownstreamRouteFinder;
using Ocelot.Library.Infrastructure.Responder;

namespace Ocelot.Library.Middleware
{
    public class DownstreamRouteFinderMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDownstreamRouteFinder _downstreamRouteFinder;
        private readonly IHttpResponder _responder;

        public DownstreamRouteFinderMiddleware(RequestDelegate next, 
            IDownstreamRouteFinder downstreamRouteFinder, 
            IHttpResponder responder)
        {
            _next = next;
            _downstreamRouteFinder = downstreamRouteFinder;
            _responder = responder;
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

            context.Items.Add("DownstreamRoute", downstreamRoute.Data);
            
            await _next.Invoke(context);
        }
    }
}