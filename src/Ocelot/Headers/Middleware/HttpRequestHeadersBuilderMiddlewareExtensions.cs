using Microsoft.AspNetCore.Builder;
using Ocelot.Middleware.Pipeline;

namespace Ocelot.Headers.Middleware
{
    public static class HttpRequestHeadersBuilderMiddlewareExtensions
    {
        public static IOcelotPipelineBuilder UseHttpRequestHeadersBuilderMiddleware(this IOcelotPipelineBuilder builder)
        {
            return builder.UseMiddleware<HttpRequestHeadersBuilderMiddleware>();
        }
    }
}
