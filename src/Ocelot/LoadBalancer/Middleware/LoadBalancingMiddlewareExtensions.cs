using Microsoft.AspNetCore.Builder;
using Ocelot.Middleware.Pipeline;

namespace Ocelot.LoadBalancer.Middleware
{
    public static class LoadBalancingMiddlewareExtensions
    {
        public static IOcelotPipelineBuilder UseLoadBalancingMiddleware(this IOcelotPipelineBuilder builder)
        {
            return builder.UseMiddleware<LoadBalancingMiddleware>();
        }
    }
}
