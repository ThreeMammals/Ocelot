using Microsoft.AspNetCore.Builder;
using Ocelot.Middleware.Pipeline;

namespace Ocelot.Cache.Middleware
{
    public static class OutputCacheMiddlewareExtensions
    {
        public static IOcelotPipelineBuilder UseOutputCacheMiddleware(this IOcelotPipelineBuilder builder)
        {
            return builder.UseMiddleware<OutputCacheMiddleware>();
        }
    }
}
