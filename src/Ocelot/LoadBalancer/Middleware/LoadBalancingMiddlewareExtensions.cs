using Microsoft.AspNetCore.Builder;

namespace Ocelot.LoadBalancer.Middleware
{
 public static class LoadBalancingMiddlewareExtensions
    {
        public static IApplicationBuilder UseLoadBalancingMiddlewareExtensions(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<LoadBalancingMiddleware>();
        }
    }
}