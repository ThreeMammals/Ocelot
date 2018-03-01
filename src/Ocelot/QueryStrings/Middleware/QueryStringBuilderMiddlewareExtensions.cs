using Microsoft.AspNetCore.Builder;
using Ocelot.Middleware.Pipeline;

namespace Ocelot.QueryStrings.Middleware
{
    public static class QueryStringBuilderMiddlewareExtensions
    {
        public static IOcelotPipelineBuilder UseQueryStringBuilderMiddleware(this IOcelotPipelineBuilder builder)
        {
            return builder.UseMiddleware<QueryStringBuilderMiddleware>();
        }
    }
}
