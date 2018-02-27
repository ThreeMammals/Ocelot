using Microsoft.AspNetCore.Builder;
using Ocelot.Middleware.Pipeline;

namespace Ocelot.Claims.Middleware
{
    public static class ClaimsBuilderMiddlewareExtensions
    {
        public static IOcelotPipelineBuilder UseClaimsBuilderMiddleware(this IOcelotPipelineBuilder builder)
        {
            return builder.UseMiddleware<ClaimsBuilderMiddleware>();
        }
    }
}
