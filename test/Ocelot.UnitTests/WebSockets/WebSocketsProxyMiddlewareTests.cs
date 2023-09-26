using Microsoft.AspNetCore.Http;
using Ocelot.Logging;
using Ocelot.WebSockets;

namespace Ocelot.UnitTests.WebSockets;

public class WebSocketsProxyMiddlewareTests
{
    private readonly WebSocketsProxyMiddleware _middleware;
    private readonly Mock<RequestDelegate> _next;
    private readonly Mock<IOcelotLoggerFactory> _factory;
    private readonly Mock<HttpContext> _context;

    public WebSocketsProxyMiddlewareTests()
    {
        _next = new Mock<RequestDelegate>();
        _factory = new Mock<IOcelotLoggerFactory>();
        _context = new Mock<HttpContext>();
        _middleware = new WebSocketsProxyMiddleware(_next.Object, _factory.Object);
    }
}
