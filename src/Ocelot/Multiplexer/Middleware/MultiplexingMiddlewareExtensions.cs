namespace Ocelot.Multiplexer
{
    using Microsoft.AspNetCore.Builder;
    using Ocelot.Middleware;

    public static class MultiplexingMiddlewareExtensions
    {
        public static IApplicationBuilder UseMultiplexingMiddleware(this IApplicationBuilder builder)
        {
            return builder.TryUseOcelotMiddleware<IOcelotMultiplexingMiddleware, MultiplexingMiddleware>();
        }
    }
}
