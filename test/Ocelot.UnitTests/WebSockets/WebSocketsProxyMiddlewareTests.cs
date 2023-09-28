using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.Builder;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.WebSockets;
using System.Net.Security;
using System.Net.WebSockets;

namespace Ocelot.UnitTests.WebSockets;

public class WebSocketsProxyMiddlewareTests
{
    private readonly WebSocketsProxyMiddleware _middleware;

    private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
    private readonly Mock<RequestDelegate> _next;
    private readonly Mock<IWebSocketsFactory> _factory;

    private readonly Mock<HttpContext> _context;
    private readonly Mock<IOcelotLogger> _logger;

    public WebSocketsProxyMiddlewareTests()
    {
        _loggerFactory = new Mock<IOcelotLoggerFactory>();
        _next = new Mock<RequestDelegate>();
        _factory = new Mock<IWebSocketsFactory>();

        _context = new Mock<HttpContext>();
        _logger = new Mock<IOcelotLogger>();
        _loggerFactory.Setup(x => x.CreateLogger<WebSocketsProxyMiddleware>())
            .Returns(_logger.Object);

        _middleware = new WebSocketsProxyMiddleware(_loggerFactory.Object, _next.Object, _factory.Object);
    }

    [Fact]
    public void ShouldIgnoreAllSslWarnings_WhenDangerousAcceptAnyServerCertificateValidatorIsTrue()
    {
        this.Given(x => x.GivenPropertyDangerousAcceptAnyServerCertificateValidator(true))
            .And(x => x.AndDoNotSetupProtocolsAndHeaders())
            .And(x => x.AndDoNotConnectReally())
            .When(x => x.WhenInvokeWithHttpContext())
            .Then(x => x.ThenIgnoredAllSslWarnings())
            .BDDfy();
    }

    private void GivenPropertyDangerousAcceptAnyServerCertificateValidator(bool enabled)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{Uri.UriSchemeWs}://localhost:12345");
        var downstream = new DownstreamRequest(request);
        var route = new DownstreamRouteBuilder()
            .WithDangerousAcceptAnyServerCertificateValidator(enabled)
            .Build();
        _context.SetupGet(x => x.Items).Returns(new Dictionary<object, object>
            {
                { "DownstreamRequest", downstream },
                { "DownstreamRoute", route },
            });

        _context.SetupGet(x => x.WebSockets.IsWebSocketRequest).Returns(true);

        _client = new Mock<IClientWebSocket>();
        _factory.Setup(x => x.CreateClient()).Returns(_client.Object);

        _client.SetupSet(x => x.Options.RemoteCertificateValidationCallback = It.IsAny<RemoteCertificateValidationCallback>())
            .Callback<RemoteCertificateValidationCallback>(value => _callback = value);

        _warning = string.Empty;
        _logger.Setup(x => x.LogWarning(It.IsAny<string>()))
            .Callback<string>(message => _warning = message);
    }

    private void AndDoNotSetupProtocolsAndHeaders()
    {
        _context.SetupGet(x => x.WebSockets.WebSocketRequestedProtocols).Returns(new List<string>());
        _context.SetupGet(x => x.Request.Headers).Returns(new HeaderDictionary());
    }

    private void AndDoNotConnectReally()
    {
        _client.Setup(x => x.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>())).Verifiable();
        var clientSocket = new Mock<WebSocket>();
        var serverSocket = new Mock<WebSocket>();
        _client.Setup(x => x.ToWebSocket()).Returns(clientSocket.Object);
        _context.Setup(x => x.WebSockets.AcceptWebSocketAsync(It.IsAny<string>())).ReturnsAsync(serverSocket.Object);

        var happyEnd = new WebSocketReceiveResult(1, WebSocketMessageType.Close, true);
        clientSocket.Setup(x => x.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(happyEnd);
        serverSocket.Setup(x => x.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(happyEnd);

        clientSocket.Setup(x => x.CloseOutputAsync(It.IsAny<WebSocketCloseStatus>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));
        serverSocket.Setup(x => x.CloseOutputAsync(It.IsAny<WebSocketCloseStatus>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));
        clientSocket.SetupGet(x => x.CloseStatus).Returns(WebSocketCloseStatus.Empty);
        serverSocket.SetupGet(x => x.CloseStatus).Returns(WebSocketCloseStatus.Empty);
    }

    private Mock<IClientWebSocket> _client;
    private RemoteCertificateValidationCallback _callback;
    private string _warning;

    private async Task WhenInvokeWithHttpContext()
    {
        await _middleware.Invoke(_context.Object);
    }

    private void ThenIgnoredAllSslWarnings()
    {
        _context.Object.Items.DownstreamRoute().DangerousAcceptAnyServerCertificateValidator
            .ShouldBeTrue();

        _logger.Verify(x => x.LogWarning(It.IsAny<string>()), Times.Once());
        _warning.ShouldNotBeNullOrEmpty();

        _client.VerifySet(x => x.Options.RemoteCertificateValidationCallback = It.IsAny<RemoteCertificateValidationCallback>(),
            Times.Once());

        _callback.ShouldNotBeNull();
        var validation = _callback.Invoke(null, null, null, SslPolicyErrors.None);
        validation.ShouldBeTrue();
    }
}
