using Microsoft.AspNetCore.Builder;

namespace Ocelot.Cache.Middleware
{
    public static class OutputCacheMiddlewareExtensions
    {
        public static IApplicationBuilder UseOutputCacheMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<OutputCacheMiddleware>();
        }
    }
}
