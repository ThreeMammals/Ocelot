namespace Ocelot.WebSockets.Middleware
{
    using Microsoft.AspNetCore.Builder;
    using Ocelot.Middleware;

    public static class WebSocketsProxyMiddlewareExtensions
    {
        public static IApplicationBuilder UseWebSocketsProxyMiddleware(this IApplicationBuilder builder)
        {
            return builder.TryUseOcelotMiddleware<IOcelotWebSocketsProxyMiddleware, WebSocketsProxyMiddleware>();
        }
    }
}
