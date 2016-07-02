using Microsoft.AspNetCore.Builder;

namespace Ocelot.ApiGateway.Middleware
{
    public static class ProxyExtensions
    {
        public static IApplicationBuilder UseProxy(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ProxyMiddleware>();
        }
    }
}