using Microsoft.AspNetCore.Http;
using Ocelot.Logging;
using Ocelot.WebSockets;

namespace Ocelot.UnitTests.WebSockets;

public class WebSocketsProxyMiddlewareTests
{
    private readonly WebSocketsProxyMiddleware _middleware;
    private readonly Mock<RequestDelegate> _next;
    private readonly Mock<IOcelotLoggerFactory> _logger;
    private readonly Mock<IWebSocketsFactory> _factory;
    private readonly Mock<HttpContext> _context;

    public WebSocketsProxyMiddlewareTests()
    {
        _next = new Mock<RequestDelegate>();
        _logger = new Mock<IOcelotLoggerFactory>();
        _factory = new Mock<IWebSocketsFactory>();
        _context = new Mock<HttpContext>();

        _middleware = new WebSocketsProxyMiddleware(_logger.Object, _next.Object, _factory.Object);
    }

    [Fact]
    public void ShouldIgnoreAllSslWarnings_WhenDangerousAcceptAnyServerCertificateValidatorIsTrue()
    {
    }
}
