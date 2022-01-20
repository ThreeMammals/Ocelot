namespace Ocelot.DownstreamRouteFinder.Middleware
{
    using Microsoft.AspNetCore.Builder;
    using Ocelot.Middleware;

    public static class DownstreamRouteFinderMiddlewareExtensions
    {
        public static IApplicationBuilder UseDownstreamRouteFinderMiddleware(this IApplicationBuilder builder)
        {
            return builder.TryUseOcelotMiddleware<IOcelotMiddleware, DownstreamRouteFinderMiddleware>();
        }
    }
}
