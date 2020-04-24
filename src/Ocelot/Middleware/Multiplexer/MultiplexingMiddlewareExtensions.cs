namespace Ocelot.DownstreamRouteFinder.Middleware
{
    using Microsoft.AspNetCore.Builder;

    public static class MultiplexingMiddlewareExtensions
    {
        public static IApplicationBuilder UseMultiplexingMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<MultiplexingMiddleware>();
        }
    }
}
