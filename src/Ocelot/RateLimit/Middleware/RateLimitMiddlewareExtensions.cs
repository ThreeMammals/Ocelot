namespace Ocelot.RateLimit.Middleware
{
    using Microsoft.AspNetCore.Builder;
    using Ocelot.Middleware;

    public static class RateLimitMiddlewareExtensions
    {
        public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
        {
            return builder.TryUseOcelotMiddleware<IOcelotClientRateLimitMiddleware, ClientRateLimitMiddleware>();
        }
    }
}
