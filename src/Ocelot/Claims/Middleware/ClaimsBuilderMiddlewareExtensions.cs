using Microsoft.AspNetCore.Builder;

namespace Ocelot.Claims.Middleware
{
    public static class ClaimsBuilderMiddlewareExtensions
    {
        public static IApplicationBuilder UseClaimsBuilderMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ClaimsBuilderMiddleware>();
        }
    }
}