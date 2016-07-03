using Microsoft.AspNetCore.Builder;

namespace Ocelot.Library.Middleware
{
    public static class ProxyExtensions
    {
        public static IApplicationBuilder UseProxy(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ProxyMiddleware>();
        }
    }
}