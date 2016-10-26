using Microsoft.AspNetCore.Builder;

namespace Ocelot.Responder.Middleware
{
    public static class HttpResponderMiddlewareExtensions
    {
        public static IApplicationBuilder UseHttpErrorResponderMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<HttpErrorResponderMiddleware>();
        }
    }
}