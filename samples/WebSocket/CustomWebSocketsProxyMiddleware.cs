using Ocelot.Logging;
using Ocelot.WebSockets;

namespace Ocelot.Samples.WebSocket;

public class CustomWebSocketsProxyMiddleware : WebSocketsProxyMiddleware
{
    protected override int DefaultWebSocketBufferSize => 65536; // 64 KB for high-throughput streams (e.g. HTTP.sys video streaming)

    public CustomWebSocketsProxyMiddleware(IOcelotLoggerFactory logging, RequestDelegate next, IWebSocketsFactory factory)
        : base(logging, next, factory) { }
}
