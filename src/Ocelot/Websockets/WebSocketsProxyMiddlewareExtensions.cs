using Ocelot.Middleware.Pipeline;

namespace Ocelot.Websockets
{
    public static class WebSocketsProxyMiddlewareExtensions
    {
        public static IOcelotPipelineBuilder UseWebSocketsProxyMiddleware(this IOcelotPipelineBuilder builder)
        {
            return builder.UseMiddleware<WebSocketsProxyMiddleware>();
        }
    }
}