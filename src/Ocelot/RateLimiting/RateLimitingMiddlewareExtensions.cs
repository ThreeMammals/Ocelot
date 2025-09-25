using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Ocelot.Middleware;

namespace Ocelot.RateLimiting.Middleware;

public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
    {
        builder.UseMiddleware<RateLimitingMiddleware>();

        //use AspNet rate limiter
#if NET7_0_OR_GREATER
        builder.UseWhen(UseAspNetRateLimiter, rateLimitedApp =>
        {
            rateLimitedApp.UseRateLimiter();
        });
#endif

        return builder;
    }

    private static bool UseAspNetRateLimiter(HttpContext httpContext)
    {
        var downstreamRoute = httpContext.Items.DownstreamRoute();
        return !string.IsNullOrWhiteSpace(downstreamRoute?.RateLimitOptions?.Policy);
    }
}
