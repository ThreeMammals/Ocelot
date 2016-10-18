using Microsoft.AspNetCore.Builder;

namespace Ocelot.RequestBuilder.Middleware
{
    public static class HttpRequestBuilderMiddlewareExtensions
    {
        public static IApplicationBuilder UseHttpRequestBuilderMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<HttpRequestBuilderMiddleware>();
        }
    }
}