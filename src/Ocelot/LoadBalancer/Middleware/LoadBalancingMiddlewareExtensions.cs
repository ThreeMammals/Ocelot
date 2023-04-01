namespace Ocelot.LoadBalancer.Middleware
{
    using Microsoft.AspNetCore.Builder;
    using Ocelot.Middleware;

    public static class LoadBalancingMiddlewareExtensions
    {
        public static IApplicationBuilder UseLoadBalancingMiddleware(this IApplicationBuilder builder)
        {
            return builder.TryUseOcelotMiddleware<IOcelotLoadBalancingMiddleware, LoadBalancingMiddleware>();
        }
    }
}
