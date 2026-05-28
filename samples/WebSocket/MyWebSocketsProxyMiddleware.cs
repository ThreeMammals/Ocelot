using Ocelot.Logging;
using Ocelot.WebSockets;

namespace Ocelot.Samples.WebSocket;

public class MyWebSocketsProxyMiddleware : WebSocketsProxyMiddleware
{
    protected override int BufferSize => 65536; // 64 KB for high-throughput streams (e.g. HTTP.sys video streaming)

    public MyWebSocketsProxyMiddleware(RequestDelegate next, IOcelotLoggerFactory logging, IWebSocketsFactory factory)
        : base(next, logging, factory) { }
}
