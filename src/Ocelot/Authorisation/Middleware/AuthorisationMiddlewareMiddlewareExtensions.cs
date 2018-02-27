using Ocelot.Middleware.Pipeline;

namespace Ocelot.Authorisation.Middleware
{
    using Microsoft.AspNetCore.Builder;

    public static class AuthorisationMiddlewareMiddlewareExtensions
    {
        public static IOcelotPipelineBuilder UseAuthorisationMiddleware(this IOcelotPipelineBuilder builder)
        {
            return builder.UseMiddleware<AuthorisationMiddleware>();
        }
    }
}
