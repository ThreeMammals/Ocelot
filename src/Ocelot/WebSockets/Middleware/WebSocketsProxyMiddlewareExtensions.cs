using Ocelot.Middleware.Pipeline;

namespace Ocelot.WebSockets.Middleware
{
    public static class WebSocketsProxyMiddlewareExtensions
    {
        public static IOcelotPipelineBuilder UseWebSocketsProxyMiddleware(this IOcelotPipelineBuilder builder)
        {
            return builder.UseMiddleware<WebSocketsProxyMiddleware>();
        }
    }
}
