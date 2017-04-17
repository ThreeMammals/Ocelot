using Microsoft.AspNetCore.Builder;

namespace Ocelot.Request.Middleware
{
    public static class HttpRequestBuilderMiddlewareExtensions
    {
        public static IApplicationBuilder UseHttpRequestBuilderMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<HttpRequestBuilderMiddleware>();
        }

        public static IApplicationBuilder UseDownstreamRequestInitialiser(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<DownstreamRequestInitialiserMiddleware>();
        }
    }
}