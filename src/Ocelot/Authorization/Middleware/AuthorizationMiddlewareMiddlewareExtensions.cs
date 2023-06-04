using Microsoft.AspNetCore.Builder;

namespace Ocelot.Authorization.Middleware
{
    public static class AuthorizationMiddlewareMiddlewareExtensions
    {
        public static IApplicationBuilder UseAuthorizationMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuthorizationMiddleware>();
        }
    }
}
