using Microsoft.AspNetCore.Builder;

namespace Ocelot.Claims.Middleware
{
    public static class ClaimsBuilderMiddlewareExtensions
    {
        public static IApplicationBuilder UseClaimsToClaimsMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ClaimsToClaimsMiddleware>();
        }
    }
}
