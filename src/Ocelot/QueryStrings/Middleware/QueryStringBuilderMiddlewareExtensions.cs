using Microsoft.AspNetCore.Builder;

namespace Ocelot.QueryStrings.Middleware
{
    public static class QueryStringBuilderMiddlewareExtensions
    {
        public static IApplicationBuilder UseQueryStringBuilderMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<QueryStringBuilderMiddleware>();
        }
    }
}