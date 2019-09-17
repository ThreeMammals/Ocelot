using Ocelot.Middleware.Pipeline;

namespace Ocelot.Authentication.Middleware
{
    public static class AuthenticationMiddlewareMiddlewareExtensions
    {
        public static IOcelotPipelineBuilder UseAuthenticationMiddleware(this IOcelotPipelineBuilder builder)
        {
            return builder.UseMiddleware<AuthenticationMiddleware>();
        }
    }
}
