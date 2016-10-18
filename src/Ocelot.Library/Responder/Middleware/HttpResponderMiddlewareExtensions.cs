using Microsoft.AspNetCore.Builder;

namespace Ocelot.Library.Responder.Middleware
{
    public static class HttpResponderMiddlewareExtensions
    {
        public static IApplicationBuilder UseHttpResponderMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<HttpResponderMiddleware>();
        }
    }
}