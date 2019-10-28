using Ocelot.Middleware.Pipeline;

namespace Ocelot.PathManipulation.Middleware
{
    public static class ClaimsToDownstreamPathMiddlewareExtensions
    {
        public static IOcelotPipelineBuilder UseClaimsToDownstreamPathMiddleware(this IOcelotPipelineBuilder builder)
        {
            return builder.UseMiddleware<ClaimsToDownstreamPathMiddleware>();
        }
    }
}
