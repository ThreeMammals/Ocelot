using Ocelot.Middleware.Pipeline;

namespace Ocelot.Security.Middleware
{
    public static class SecurityMiddlewareExtensions
    {
        public static IOcelotPipelineBuilder UseSecurityMiddleware(this IOcelotPipelineBuilder builder)
        {
            return builder.UseMiddleware<SecurityMiddleware>();
        }
    }
}
