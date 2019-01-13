using Ocelot.Middleware.Pipeline;

namespace Ocelot.Claims.Middleware
{
    public static class ClaimsBuilderMiddlewareExtensions
    {
        public static IOcelotPipelineBuilder UseClaimsToClaimsMiddleware(this IOcelotPipelineBuilder builder)
        {
            return builder.UseMiddleware<ClaimsToClaimsMiddleware>();
        }
    }
}
