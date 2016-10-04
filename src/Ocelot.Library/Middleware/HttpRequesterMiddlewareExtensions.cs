using Microsoft.AspNetCore.Builder;

namespace Ocelot.Library.Middleware
{
    public static class HttpRequesterMiddlewareExtensions
    {
        public static IApplicationBuilder UseHttpRequesterMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<HttpRequesterMiddleware>();
        }
    }
}