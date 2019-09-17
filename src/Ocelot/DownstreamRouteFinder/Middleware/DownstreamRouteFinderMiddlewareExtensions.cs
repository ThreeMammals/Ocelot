using Microsoft.AspNetCore.Builder;
using Ocelot.Middleware.Pipeline;

namespace Ocelot.DownstreamRouteFinder.Middleware
{
    public static class DownstreamRouteFinderMiddlewareExtensions
    {
        public static IOcelotPipelineBuilder UseDownstreamRouteFinderMiddleware(this IOcelotPipelineBuilder builder)
        {
            return builder.UseMiddleware<DownstreamRouteFinderMiddleware>();
        }
    }
}
