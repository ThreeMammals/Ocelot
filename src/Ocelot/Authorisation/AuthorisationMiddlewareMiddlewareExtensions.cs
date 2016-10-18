using Microsoft.AspNetCore.Builder;

namespace Ocelot.Authorisation
{
    public static class AuthorisationMiddlewareMiddlewareExtensions
    {
        public static IApplicationBuilder UseAuthorisationMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuthorisationMiddleware>();
        }
    }
}