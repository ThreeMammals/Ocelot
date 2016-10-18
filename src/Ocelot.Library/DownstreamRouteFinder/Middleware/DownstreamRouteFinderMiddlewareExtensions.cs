using Microsoft.AspNetCore.Builder;

namespace Ocelot.Library.DownstreamRouteFinder.Middleware
{
    public static class DownstreamRouteFinderMiddlewareExtensions
    {
        public static IApplicationBuilder UseDownstreamRouteFinderMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<DownstreamRouteFinderMiddleware>();
        }
    }
}