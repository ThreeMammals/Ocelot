using Microsoft.AspNetCore.Builder;
using Ocelot.Middleware.Pipeline;

namespace Ocelot.Responder.Middleware
{
    public static class ResponderMiddlewareExtensions
    {
        public static IOcelotPipelineBuilder UseResponderMiddleware(this IOcelotPipelineBuilder builder)
        {
            return builder.UseMiddleware<ResponderMiddleware>();
        }
    }
}
