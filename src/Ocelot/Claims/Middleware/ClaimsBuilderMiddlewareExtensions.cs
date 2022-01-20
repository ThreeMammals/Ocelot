namespace Ocelot.Claims.Middleware
{
    using Microsoft.AspNetCore.Builder;
    using Ocelot.Middleware;

    public static class ClaimsBuilderMiddlewareExtensions
    {
        public static IApplicationBuilder UseClaimsToClaimsMiddleware(this IApplicationBuilder builder)
        {
            return builder.TryUseOcelotMiddleware<IOcelotMiddleware, ClaimsToClaimsMiddleware>();
        }
    }
}
