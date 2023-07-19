using Microsoft.AspNetCore.Builder;

namespace Ocelot.Middleware
{
    public static class DownstreamContextMiddlewareExtensions
    {
        public static IApplicationBuilder UseDownstreamContextMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ConfigurationMiddleware>();
        }
    }
}
