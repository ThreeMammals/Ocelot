using Microsoft.AspNetCore.Builder;

namespace Ocelot.HeaderBuilder.Middleware
{
    public static class HttpRequestHeadersBuilderMiddlewareExtensions
    {
        public static IApplicationBuilder UseHttpRequestHeadersBuilderMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<HttpRequestHeadersBuilderMiddleware>();
        }
    }
}