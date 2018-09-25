using Microsoft.AspNetCore.Builder;
using Ocelot.Middleware.Pipeline;

namespace Ocelot.Claims.Middleware
{
    public static class ClaimsToClaimsMiddlewareExtensions
    {
        public static IOcelotPipelineBuilder UseClaimsToClaimsMiddleware(this IOcelotPipelineBuilder builder)
        {
            return builder.UseMiddleware<ClaimsToClaimsMiddleware>();
        }
    }
}
