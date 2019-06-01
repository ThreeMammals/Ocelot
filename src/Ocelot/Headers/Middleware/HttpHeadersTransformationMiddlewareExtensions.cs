using Microsoft.AspNetCore.Builder;
using Ocelot.Middleware.Pipeline;

namespace Ocelot.Headers.Middleware
{
    public static class HttpHeadersTransformationMiddlewareExtensions
    {
        public static IOcelotPipelineBuilder UseHttpHeadersTransformationMiddleware(this IOcelotPipelineBuilder builder)
        {
            return builder.UseMiddleware<HttpHeadersTransformationMiddleware>();
        }
    }
}
