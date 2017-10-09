using Microsoft.AspNetCore.Builder;

namespace Ocelot.Request.Middleware
{
    public static class RequestBuilderMiddlewareExtensions
    {
        public static IApplicationBuilder UseHttpRequestBuilderMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<HttpRequestBuilderMiddleware>();
        }

        public static IApplicationBuilder UseDownstreamRequestInitialiser(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<DownstreamRequestInitialiserMiddleware>();
        }

        public static IApplicationBuilder UseRequestBuilderMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestBuilderMiddleware>(builder);
        }
    }
}