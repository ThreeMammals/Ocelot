using Microsoft.AspNetCore.Builder;

namespace Ocelot.Headers.Middleware
{
    public static class HttpHeadersTransformationMiddlewareExtensions
    {
        public static IApplicationBuilder UseHttpHeadersTransformationMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<HttpHeadersTransformationMiddleware>();
        }
    }
}