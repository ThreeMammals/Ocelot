using Microsoft.AspNetCore.Builder;

namespace Ocelot.RequestId.Middleware
{
    public static class RequestIdMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestIdMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ReRouteRequestIdMiddleware>();
        }
    }
}