using Microsoft.AspNetCore.Builder;
using Ocelot.Middleware.Pipeline;

namespace Ocelot.Requester.Middleware
{
    public static class HttpRequesterMiddlewareExtensions
    {
        public static IOcelotPipelineBuilder UseHttpRequesterMiddleware(this IOcelotPipelineBuilder builder)
        {
            return builder.UseMiddleware<HttpRequesterMiddleware>();
        }
    }
}
