using Microsoft.AspNetCore.Builder;
using Ocelot.Middleware.Pipeline;

namespace Ocelot.DownstreamUrlCreator.Middleware
{
    public static class DownstreamUrlCreatorMiddlewareExtensions
    {
        public static IOcelotPipelineBuilder UseDownstreamUrlCreatorMiddleware(this IOcelotPipelineBuilder builder)
        {
            return builder.UseMiddleware<DownstreamUrlCreatorMiddleware>();
        }
    }
}
