using Microsoft.AspNetCore.Builder;

namespace Ocelot.LoadBalancer.Middleware
{
 public static class LoadBalancingMiddlewareExtensions
    {
        public static IApplicationBuilder UseLoadBalancingMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<LoadBalancingMiddleware>();
        }
    }
}