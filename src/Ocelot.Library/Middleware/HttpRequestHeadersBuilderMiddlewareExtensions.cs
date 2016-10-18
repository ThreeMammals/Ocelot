namespace Ocelot.Library.Middleware
{
    using Microsoft.AspNetCore.Builder;

    public static class HttpRequestHeadersBuilderMiddlewareExtensions
    {
        public static IApplicationBuilder UseHttpRequestHeadersBuilderMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<HttpRequestHeadersBuilderMiddleware>();
        }
    }
}