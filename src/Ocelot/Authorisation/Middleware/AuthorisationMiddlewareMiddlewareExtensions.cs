using Ocelot.Middleware.Pipeline;

namespace Ocelot.Authorisation.Middleware
{
    public static class AuthorisationMiddlewareMiddlewareExtensions
    {
        public static IOcelotPipelineBuilder UseAuthorisationMiddleware(this IOcelotPipelineBuilder builder)
        {
            return builder.UseMiddleware<AuthorisationMiddleware>();
        }
    }
}
