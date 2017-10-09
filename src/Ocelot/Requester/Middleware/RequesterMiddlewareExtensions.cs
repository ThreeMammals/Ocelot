using Microsoft.AspNetCore.Builder;

namespace Ocelot.Requester.Middleware
{
    public static class RequesterMiddlewareExtensions
    {
        public static IApplicationBuilder UseHttpRequesterMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<HttpRequesterMiddleware>();
        }

        public static IApplicationBuilder UseRequesterMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequesterMiddleware>(builder);
        }
    }
}