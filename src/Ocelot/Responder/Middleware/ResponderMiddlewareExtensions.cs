using Microsoft.AspNetCore.Builder;

namespace Ocelot.Responder.Middleware
{
    public static class ResponderMiddlewareExtensions
    {
        public static IApplicationBuilder UseResponderMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ResponderMiddleware>();
        }
    }
}