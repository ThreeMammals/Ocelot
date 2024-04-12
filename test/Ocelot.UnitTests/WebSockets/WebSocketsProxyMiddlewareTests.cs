using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.Builder;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.WebSockets;
using System.Net.Security;
using System.Net.WebSockets;

namespace Ocelot.UnitTests.WebSockets;

public class WebSocketsProxyMiddlewareTests : UnitTest
{
    private readonly WebSocketsProxyMiddleware _middleware;

    private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
    private readonly Mock<RequestDelegate> _next;
    private readonly Mock<IWebSocketsFactory> _factory;

    private readonly Mock<HttpContext> _context;
    private readonly Mock<IOcelotLogger> _logger;
    private readonly Mock<IClientWebSocket> _client;

    public WebSocketsProxyMiddlewareTests()
    {
        _loggerFactory = new Mock<IOcelotLoggerFactory>();
        _next = new Mock<RequestDelegate>();
        _factory = new Mock<IWebSocketsFactory>();

        _context = new Mock<HttpContext>();
        _context.SetupGet(x => x.WebSockets.IsWebSocketRequest).Returns(true);

        _logger = new Mock<IOcelotLogger>();
        _loggerFactory.Setup(x => x.CreateLogger<WebSocketsProxyMiddleware>())
            .Returns(_logger.Object);

        _middleware = new WebSocketsProxyMiddleware(_loggerFactory.Object, _next.Object, _factory.Object);

        _client = new Mock<IClientWebSocket>();
        _factory.Setup(x => x.CreateClient()).Returns(_client.Object);
    }

    [Fact]
    public void ShouldIgnoreAllSslWarningsWhenDangerousAcceptAnyServerCertificateValidatorIsTrue()
    {
        List<object> actual = new();
        this.Given(x => x.GivenPropertyDangerousAcceptAnyServerCertificateValidator(true, actual))
            .And(x => x.AndDoNotSetupProtocolsAndHeaders())
            .And(x => x.AndDoNotConnectReally(null))
            .When(x => x.WhenInvokeWithHttpContext())
            .Then(x => x.ThenIgnoredAllSslWarnings(actual))
            .BDDfy();
    }

    private void GivenPropertyDangerousAcceptAnyServerCertificateValidator(bool enabled, List<object> actual)
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

        _client.SetupSet(x => x.Options.RemoteCertificateValidationCallback = It.IsAny<RemoteCertificateValidationCallback>())
            .Callback<RemoteCertificateValidationCallback>(actual.Add);

        _logger.Setup(x => x.LogWarning(It.IsAny<Func<string>>()))
            .Callback<Func<string>>(y => actual.Add(y.Invoke()));
    }

    private void AndDoNotSetupProtocolsAndHeaders()
    {
        _context.SetupGet(x => x.WebSockets.WebSocketRequestedProtocols).Returns(new List<string>());
        _context.SetupGet(x => x.Request.Headers).Returns(new HeaderDictionary());
    }

    private void AndDoNotConnectReally(Action<Uri, CancellationToken> callbackConnectAsync)
    {
        Action<Uri, CancellationToken> doNothing = (u, t) => { };
        _client.Setup(x => x.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
            .Callback(callbackConnectAsync ?? doNothing);
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

    private async Task WhenInvokeWithHttpContext()
    {
        await _middleware.Invoke(_context.Object);
    }

    private void ThenIgnoredAllSslWarnings(List<object> actual)
    {
        var route = _context.Object.Items.DownstreamRoute();
        var request = _context.Object.Items.DownstreamRequest();
        route.DangerousAcceptAnyServerCertificateValidator.ShouldBeTrue();

        _logger.Verify(x => x.LogWarning(It.IsAny<Func<string>>()), Times.Once());
        var warning = actual.Last() as string;
        warning.ShouldNotBeNullOrEmpty();
        var expectedWarning = string.Format(WebSocketsProxyMiddleware.IgnoredSslWarningFormat, route.UpstreamPathTemplate, route.DownstreamPathTemplate);
        warning.ShouldBe(expectedWarning);

        _client.VerifySet(x => x.Options.RemoteCertificateValidationCallback = It.IsAny<RemoteCertificateValidationCallback>(),
            Times.Once());

        var callback = actual.First() as RemoteCertificateValidationCallback;
        callback.ShouldNotBeNull();
        var validation = callback.Invoke(null, null, null, SslPolicyErrors.None);
        validation.ShouldBeTrue();
    }

    [Theory]
    [InlineData("http", "ws")]
    [InlineData("https", "wss")]
    [InlineData("ftp", "ftp")]
    public void ShouldReplaceNonWsSchemes(string scheme, string expectedScheme)
    {
        List<object> actual = new();
        this.Given(x => x.GivenNonWebsocketScheme(scheme, actual))
            .And(x => x.AndDoNotSetupProtocolsAndHeaders())
            .And(x => x.AndDoNotConnectReally((uri, token) => actual.Add(uri)))
            .When(x => x.WhenInvokeWithHttpContext())
            .Then(x => x.ThenNonWsSchemesAreReplaced(scheme, expectedScheme, actual))
            .BDDfy();
    }

    private void GivenNonWebsocketScheme(string scheme, List<object> actual)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{scheme}://localhost:12345");
        var request = new DownstreamRequest(requestMessage);
        var route = new DownstreamRouteBuilder().Build();
        var items = new Dictionary<object, object>
        {
            { "DownstreamRequest", request },
            { "DownstreamRoute", route },
        };
        _context.SetupGet(x => x.Items).Returns(items);

        _logger.Setup(x => x.LogWarning(It.IsAny<Func<string>>()))
            .Callback<Func<string>>(myFunc => actual.Add(myFunc.Invoke()));
    }

    private void ThenNonWsSchemesAreReplaced(string scheme, string expectedScheme, List<object> actual)
    {
        var route = _context.Object.Items.DownstreamRoute();
        var request = _context.Object.Items.DownstreamRequest();
        route.DangerousAcceptAnyServerCertificateValidator.ShouldBeFalse();

        _logger.Verify(x => x.LogWarning(It.IsAny<Func<string>>()), Times.Once());
        var warning = actual.First() as string;
        warning.ShouldNotBeNullOrEmpty();
        warning.ShouldContain($"'{scheme}'");
        var expectedWarning = string.Format(WebSocketsProxyMiddleware.InvalidSchemeWarningFormat, scheme, request.ToUri().Replace(expectedScheme, scheme));
        warning.ShouldBe(expectedWarning);

        request.Scheme.ShouldBe(expectedScheme);
        ((Uri)actual.Last()).Scheme.ShouldBe(expectedScheme);
    }
}
