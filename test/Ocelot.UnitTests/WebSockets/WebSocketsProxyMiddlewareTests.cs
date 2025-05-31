using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.WebSockets;
using System.Linq.Expressions;
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
    public async Task Proxy_NotIsWebSocketRequest_ThrownException()
    {
        // Arrange
        List<object> messages = new();
        GivenNonWebsocketScheme(Uri.UriSchemeHttps, messages);
        _context.SetupGet(x => x.WebSockets.IsWebSocketRequest).Returns(false);

        // Act
        Task action() => _middleware.Invoke(_context.Object);

        // Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(action);
        Assert.NotNull(ex);
    }

    [Fact]
    public async Task Proxy_ThereAreWebSocketRequestedProtocols_AddedSubProtocols()
    {
        // Arrange
        List<object> messages = new();
        GivenPropertyDangerousAcceptAnyServerCertificateValidator(false, messages);
        AndSetupProtocolsAndHeaders(
            new() { Uri.UriSchemeHttps, Uri.UriSchemeWs, Uri.UriSchemeWss },
            null);
        AndDoNotConnectReally(null);
        var options = new Mock<IClientWebSocketOptions>();
        _client.SetupGet(x => x.Options)
            .Returns(options.Object).Verifiable();
        var actualProtos = new List<string>();
        options.Setup(x => x.AddSubProtocol(It.IsAny<string>()))
            .Callback<string>(actualProtos.Add).Verifiable();

        // Act
        await _middleware.Invoke(_context.Object);

        // Assert
        _client.VerifyGet(x => x.Options, Times.Exactly(3));
        options.Verify(x => x.AddSubProtocol(It.IsAny<string>()), Times.Exactly(3));
        Assert.Equal(3, actualProtos.Count);
    }

    [Fact]
    public async Task Proxy_ThereAreHeaders_SetRequestHeaders()
    {
        // Arrange
        List<object> messages = new();
        HeaderDictionary headers = new()
        {
            { "TestMe", nameof(Proxy_ThereAreHeaders_SetRequestHeaders) },
        };
        GivenPropertyDangerousAcceptAnyServerCertificateValidator(false, messages);
        AndSetupProtocolsAndHeaders(null, headers);
        AndDoNotConnectReally(null);
        var options = new Mock<IClientWebSocketOptions>();
        _client.SetupGet(x => x.Options).Returns(options.Object).Verifiable();
        var actual = new Dictionary<string, string>();
        options.Setup(x => x.SetRequestHeader(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>(actual.Add).Verifiable();

        // Act
        await _middleware.Invoke(_context.Object);

        // Assert
        _client.VerifyGet(x => x.Options, Times.Exactly(1));
        options.Verify(x => x.SetRequestHeader(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(1));
        Assert.Single(actual);
        Assert.True(actual.ContainsKey("TestMe"));
        Assert.Equal(nameof(Proxy_ThereAreHeaders_SetRequestHeaders), actual["TestMe"]);
    }

    [Fact]
    public async Task Proxy_ThereAreHeaders_ThrownExceptionButCaughtIt()
    {
        // Arrange
        List<object> messages = new();
        HeaderDictionary headers = new()
        {
            { "TestMe", nameof(Proxy_ThereAreHeaders_ThrownExceptionButCaughtIt) },
        };
        GivenPropertyDangerousAcceptAnyServerCertificateValidator(false, messages);
        AndSetupProtocolsAndHeaders(null, headers);
        AndDoNotConnectReally(null);
        var options = new Mock<IClientWebSocketOptions>();
        _client.SetupGet(x => x.Options).Returns(options.Object).Verifiable();
        var actual = new Dictionary<string, string>();
        options.Setup(x => x.SetRequestHeader(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new ArgumentException()); // !!!

        // Act
        await _middleware.Invoke(_context.Object);

        // Assert
        _client.VerifyGet(x => x.Options, Times.Exactly(1));
        options.Verify(x => x.SetRequestHeader(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(1));
        Assert.Empty(actual);
    }

    [Fact]
    [Trait("Bug", "1375 1237 925 920")]
    [Trait("PR", "1377")] // https://github.com/ThreeMammals/Ocelot/pull/1377
    public async Task ShouldIgnoreAllSslWarningsWhenDangerousAcceptAnyServerCertificateValidatorIsTrue()
    {
        // Arrange
        List<object> actual = new();
        GivenPropertyDangerousAcceptAnyServerCertificateValidator(true, actual);
        AndDoNotSetupProtocolsAndHeaders();
        AndDoNotConnectReally(null);

        // Act
        await _middleware.Invoke(_context.Object);

        // Assert
        ThenIgnoredAllSslWarnings(actual);
    }

    private void GivenPropertyDangerousAcceptAnyServerCertificateValidator(bool enabled, List<object> messages)
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            new UriBuilder(Uri.UriSchemeWs, "localhost", PortFinder.GetRandomPort()).Uri);
        var downstream = new DownstreamRequest(request);
        var route = new DownstreamRouteBuilder()
            .WithDangerousAcceptAnyServerCertificateValidator(enabled)
            .Build();
        _context.SetupGet(x => x.Items).Returns(new Dictionary<object, object>
        {
            { nameof(DownstreamRequest), downstream },
            { nameof(DownstreamRoute), route },
        });
        _client.SetupSet(x => x.Options.RemoteCertificateValidationCallback = It.IsAny<RemoteCertificateValidationCallback>())
            .Callback<RemoteCertificateValidationCallback>(messages.Add);
        _logger.Setup(x => x.LogWarning(It.IsAny<Func<string>>()))
            .Callback<Func<string>>(y => messages.Add(y.Invoke()));
    }

    private void AndDoNotSetupProtocolsAndHeaders() => AndSetupProtocolsAndHeaders(null, null);
    private void AndSetupProtocolsAndHeaders(List<string> protos = null, HeaderDictionary headers = null)
    {
        _context.SetupGet(x => x.WebSockets.WebSocketRequestedProtocols).Returns(protos ?? new());
        _context.SetupGet(x => x.Request.Headers).Returns(headers ?? new());
    }

    private Mock<WebSocket> DoNotConnectReally(Action<Uri, CancellationToken> callbackConnectAsync, out Mock<WebSocket> server)
    {
        Action<Uri, CancellationToken> doNothing = (u, t) => { };
        _client.Setup(x => x.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
            .Callback(callbackConnectAsync ?? doNothing);
        var clientSocket = new Mock<WebSocket>();
        var serverSocket = new Mock<WebSocket>();
        _client.Setup(x => x.ToWebSocket()).Returns(clientSocket.Object);
        _context.Setup(x => x.WebSockets.AcceptWebSocketAsync(It.IsAny<string>())).ReturnsAsync(serverSocket.Object);
        server = serverSocket;
        return clientSocket;
    }

    private void AndDoNotConnectReally(Action<Uri, CancellationToken> callbackConnectAsync)
    {
        var clientSocket = DoNotConnectReally(callbackConnectAsync, out var serverSocket);

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
    [Trait("Bug", "1509 1683")]
    [Trait("PR", "1689")] // https://github.com/ThreeMammals/Ocelot/pull/1689
    [InlineData("http", "ws")]
    [InlineData("https", "wss")]
    [InlineData("ftp", "ftp")]
    public async Task ShouldReplaceNonWsSchemes(string scheme, string expectedScheme)
    {
        // Arrange
        List<object> actual = new();
        GivenNonWebsocketScheme(scheme, actual);
        AndDoNotSetupProtocolsAndHeaders();
        AndDoNotConnectReally((uri, token) => actual.Add(uri));

        // Act
        await _middleware.Invoke(_context.Object);

        // Assert
        ThenNonWsSchemesAreReplaced(scheme, expectedScheme, actual);
    }

    private void GivenNonWebsocketScheme(string scheme, List<object> messages)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{scheme}://localhost:12345");
        var request = new DownstreamRequest(requestMessage);
        var route = new DownstreamRouteBuilder().Build();
        var items = new Dictionary<object, object>
        {
            { nameof(DownstreamRequest), request },
            { nameof(DownstreamRoute), route },
        };
        _context.SetupGet(x => x.Items).Returns(items);

        _logger.Setup(x => x.LogWarning(It.IsAny<Func<string>>()))
            .Callback<Func<string>>(myFunc => messages.Add(myFunc.Invoke()));
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

    private static WebSocketCloseStatus[] AndBothSocketsGenerateExceptionWhenReceiveAsync(Mock<WebSocket> clientSocket, Mock<WebSocket> serverSocket, Exception error, Func<Task> closing)
    {
        var actual = new WebSocketCloseStatus[2];
        var cresult = clientSocket.Setup(x => x.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(error);
        clientSocket.SetupGet(x => x.State).Returns(WebSocketState.Open);
        clientSocket.Setup(x => x.CloseOutputAsync(It.IsAny<WebSocketCloseStatus>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(closing)
            .Callback<WebSocketCloseStatus, string, CancellationToken>((s, d, t) => actual[0] = s);

        var sresult = serverSocket.Setup(x => x.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(error);
        serverSocket.SetupGet(x => x.State).Returns(WebSocketState.Open);
        serverSocket.Setup(x => x.CloseOutputAsync(It.IsAny<WebSocketCloseStatus>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(closing)
            .Callback<WebSocketCloseStatus, string, CancellationToken>((s, d, t) => actual[1] = s);
        return actual;
    }

    private static void ThenBothSocketsClosedOutputTimes(Mock<WebSocket> clientSocket, Mock<WebSocket> serverSocket, Times howMany)
    {
        clientSocket.Verify(
            x => x.CloseOutputAsync(It.IsAny<WebSocketCloseStatus>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            howMany);
        serverSocket.Verify(
            x => x.CloseOutputAsync(It.IsAny<WebSocketCloseStatus>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            howMany);
    }

    [Fact]
    [Trait("Bug", "930")]
    [Trait("PR", "2091")] // https://github.com/ThreeMammals/Ocelot/pull/2091
    public async Task PumpWebSocket_OperationCanceledException_ClosedDestinationSocket()
    {
        // Arrange
        bool closed = false;
        Task Closing()
        {
            closed = true;
            return Task.CompletedTask;
        }

        var messages = new List<object>();
        GivenPropertyDangerousAcceptAnyServerCertificateValidator(false, messages);
        AndDoNotSetupProtocolsAndHeaders();
        var clientSocket = DoNotConnectReally(null, out var serverSocket);
        var error = new OperationCanceledException();
        var actual = AndBothSocketsGenerateExceptionWhenReceiveAsync(clientSocket, serverSocket, error, Closing);

        // Act
        await _middleware.Invoke(_context.Object);

        // Assert
        ThenBothSocketsClosedOutputTimes(clientSocket, serverSocket, Times.Once());
        Assert.True(closed);
        Assert.All(actual, s => Assert.Equal(WebSocketCloseStatus.EndpointUnavailable, s));
    }

    [Fact]
    [Trait("Bug", "930")]
    [Trait("PR", "2091")] // https://github.com/ThreeMammals/Ocelot/pull/2091
    public async Task PumpWebSocket_WebSocketException_ClosedDestinationSocket()
    {
        // Arrange
        bool closed = false;
        Task Closing()
        {
            closed = true;
            return Task.CompletedTask;
        }

        var messages = new List<object>();
        GivenPropertyDangerousAcceptAnyServerCertificateValidator(false, messages);
        AndDoNotSetupProtocolsAndHeaders();
        var clientSocket = DoNotConnectReally(null, out var serverSocket);
        var error = new WebSocketException(WebSocketError.ConnectionClosedPrematurely);
        var actual = AndBothSocketsGenerateExceptionWhenReceiveAsync(clientSocket, serverSocket, error, Closing);

        // Act
        await _middleware.Invoke(_context.Object);

        // Assert
        ThenBothSocketsClosedOutputTimes(clientSocket, serverSocket, Times.Once());
        Assert.True(closed);
        Assert.All(actual, s => Assert.Equal(WebSocketCloseStatus.EndpointUnavailable, s));
    }

    [Fact]
    [Trait("Bug", "930")]
    [Trait("PR", "2091")] // https://github.com/ThreeMammals/Ocelot/pull/2091
    public async Task PumpWebSocket_WebSocketExceptionAndNotConnectionClosedPrematurely_Rethrown()
    {
        // Arrange
        bool closed = false;
        Task Closing()
        {
            closed = true;
            return Task.CompletedTask;
        }

        var messages = new List<object>();
        GivenPropertyDangerousAcceptAnyServerCertificateValidator(false, messages);
        AndDoNotSetupProtocolsAndHeaders();
        var clientSocket = DoNotConnectReally(null, out var serverSocket);
        var error = new WebSocketException(WebSocketError.InvalidState); // !!!
        var actual = AndBothSocketsGenerateExceptionWhenReceiveAsync(clientSocket, serverSocket, error, Closing);

        // Act
        Task action() => _middleware.Invoke(_context.Object);

        // Assert
        var ex = await Assert.ThrowsAsync<WebSocketException>(action);
        Assert.Equal(WebSocketError.InvalidState, ex.WebSocketErrorCode);
        ThenBothSocketsClosedOutputTimes(clientSocket, serverSocket, Times.Never());
        Assert.False(closed);
        Assert.All(actual, s => Assert.Equal(0, (int)s));
    }

    [Fact]
    [Trait("Bug", "930")]
    [Trait("PR", "2091")] // https://github.com/ThreeMammals/Ocelot/pull/2091
    public async Task PumpWebSocket_IsOpen_SentToDestination()
    {
        // Arrange
        var messages = new List<object>();
        GivenPropertyDangerousAcceptAnyServerCertificateValidator(false, messages);
        AndDoNotSetupProtocolsAndHeaders();
        var clientSocket = DoNotConnectReally(null, out var serverSocket);

        int clientCount = 0, serverCount = 0;
        var open = new WebSocketReceiveResult(1, WebSocketMessageType.Binary, true);
        var close = new WebSocketReceiveResult(1, WebSocketMessageType.Close, true);
        clientSocket.Setup(x => x.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => clientCount++ < 1 ? open : close);
        serverSocket.Setup(x => x.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => serverCount++ < 1 ? open : close);
        clientSocket.SetupGet(x => x.State).Returns(() => clientCount < 1 ? WebSocketState.Open : WebSocketState.Closed);
        serverSocket.SetupGet(x => x.State).Returns(() => serverCount < 1 ? WebSocketState.Open : WebSocketState.Closed);
        clientSocket.Setup(x => x.SendAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<WebSocketMessageType>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()));
        serverSocket.Setup(x => x.SendAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<WebSocketMessageType>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()));
        clientSocket.Setup(x => x.CloseOutputAsync(It.IsAny<WebSocketCloseStatus>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));
        serverSocket.Setup(x => x.CloseOutputAsync(It.IsAny<WebSocketCloseStatus>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));
        clientSocket.SetupGet(x => x.CloseStatus).Returns(WebSocketCloseStatus.Empty);
        serverSocket.SetupGet(x => x.CloseStatus).Returns(WebSocketCloseStatus.Empty);
        clientSocket.SetupGet(x => x.CloseStatusDescription).Returns("closed");
        serverSocket.SetupGet(x => x.CloseStatusDescription).Returns("closed");

        // Act
        await _middleware.Invoke(_context.Object);

        // Assert
        Expression<Func<WebSocket, Task>> closeOutputAsync =
            x => x.CloseOutputAsync(It.IsAny<WebSocketCloseStatus>(), It.IsAny<string>(), It.IsAny<CancellationToken>());
        Expression<Func<WebSocket, Task>> sendAsync =
            x => x.SendAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<WebSocketMessageType>(), It.IsAny<bool>(), It.IsAny<CancellationToken>());
        clientSocket.Verify(closeOutputAsync, Times.Never());
        serverSocket.Verify(closeOutputAsync, Times.Once());
        clientSocket.Verify(sendAsync, Times.Never());
        serverSocket.Verify(sendAsync, Times.Once());
        Assert.Equal(2, clientCount);
        Assert.Equal(2, serverCount);
    }
}
