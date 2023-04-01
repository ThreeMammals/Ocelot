namespace Ocelot.Authentication.Middleware
{
    using Microsoft.AspNetCore.Builder;
    using Ocelot.Middleware;

    public static class AuthenticationMiddlewareMiddlewareExtensions
    {
        public static IApplicationBuilder UseAuthenticationMiddleware(this IApplicationBuilder builder)
        {
            return builder.TryUseOcelotMiddleware<IOcelotAuthenticationMiddleware, AuthenticationMiddleware>();
        }
    }
}
