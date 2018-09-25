using Microsoft.AspNetCore.Builder;
using Ocelot.Middleware.Pipeline;

namespace Ocelot.Headers.Middleware
{
    public static class ClaimsToHeadersMiddlewareExtensions
    {
        public static IOcelotPipelineBuilder UseClaimsToHeadersMiddleware(this IOcelotPipelineBuilder builder)
        {
            return builder.UseMiddleware<ClaimsToHeadersMiddleware>();
        }
    }
}
