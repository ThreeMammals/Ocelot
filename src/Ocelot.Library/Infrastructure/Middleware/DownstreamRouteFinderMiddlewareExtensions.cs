using Microsoft.AspNetCore.Builder;

namespace Ocelot.Library.Infrastructure.Middleware
{
    public static class DownstreamRouteFinderMiddlewareExtensions
    {
        public static IApplicationBuilder UseDownstreamRouteFinderMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<DownstreamRouteFinderMiddleware>();
        }
    }
}