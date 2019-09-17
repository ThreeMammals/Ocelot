using Microsoft.AspNetCore.Builder;
using Ocelot.Middleware.Pipeline;

namespace Ocelot.RequestId.Middleware
{
    public static class RequestIdMiddlewareExtensions
    {
        public static IOcelotPipelineBuilder UseRequestIdMiddleware(this IOcelotPipelineBuilder builder)
        {
            return builder.UseMiddleware<ReRouteRequestIdMiddleware>();
        }
    }
}
