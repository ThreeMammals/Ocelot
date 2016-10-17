namespace Ocelot.Library.Middleware
{
    using Microsoft.AspNetCore.Builder;

    public static class HttpRequestBuilderMiddlewareExtensions
    {
        public static IApplicationBuilder UseHttpRequestBuilderMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<HttpRequestBuilderMiddleware>();
        }
    }
}