using Ocelot.Middleware.Pipeline;

namespace Ocelot.RateLimit.Middleware
{
    public static class RateLimitMiddlewareExtensions
    {
        public static IOcelotPipelineBuilder UseRateLimiting(this IOcelotPipelineBuilder builder)
        {
            return builder.UseMiddleware<ClientRateLimitMiddleware>();
        }
    }
}
