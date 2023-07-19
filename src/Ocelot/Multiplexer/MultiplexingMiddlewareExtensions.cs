using Microsoft.AspNetCore.Builder;

namespace Ocelot.Multiplexer
{
    public static class MultiplexingMiddlewareExtensions
    {
        public static IApplicationBuilder UseMultiplexingMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<MultiplexingMiddleware>();
        }
    }
}
