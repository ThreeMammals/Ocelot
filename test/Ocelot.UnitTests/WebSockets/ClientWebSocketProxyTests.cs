using Ocelot.WebSockets;
using System.Net.WebSockets;

namespace Ocelot.UnitTests.WebSockets;

public sealed class ClientWebSocketProxyTests : UnitTest, IDisposable
{
    private readonly ClientWebSocketProxy _proxy;
    private readonly Mock<WebSocket> _socket;
    private readonly Mock<IClientWebSocketConnector> _connector;

    public ClientWebSocketProxyTests()
    {
        _socket = new Mock<WebSocket>();
        _connector = new Mock<IClientWebSocketConnector>();
        _proxy = new(_socket.Object, _connector.Object);
    }

    public void Dispose() => _proxy.Dispose();

    [Fact]
    public void ToWebSocket_NoCasting()
    {
        // Arrange, Act
        var actual = _proxy.ToWebSocket();

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(_socket.Object, actual);
    }

    [Fact]
    public void Options_Proxied()
    {
        // Arrange
        var options = new Mock<IClientWebSocketOptions>();
        _connector.SetupGet(x => x.Options)
            .Returns(options.Object).Verifiable();

        // Act
        var actual = _proxy.Options;

        // Assert
        Assert.NotNull(actual);
        _connector.VerifyGet(x => x.Options, Times.Once());
    }

    [Fact]
    public async Task ConnectAsync_Proxied()
    {
        // Arrange
        var options = new Mock<IClientWebSocketOptions>();
        _connector.Setup(x => x.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask).Verifiable();

        // Act
        await _proxy.ConnectAsync(new("https://ocelot.net"), CancellationToken.None);

        // Assert
        _connector.Verify(
            x => x.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public void CloseStatus_Proxied()
    {
        // Arrange
        _socket.SetupGet(x => x.CloseStatus)
            .Returns(WebSocketCloseStatus.Empty).Verifiable();

        // Act
        var actual = _proxy.CloseStatus;

        // Assert
        Assert.NotNull(actual);
        _socket.VerifyGet(x => x.CloseStatus, Times.Once());
    }

    [Fact]
    public void CloseStatusDescription_Proxied()
    {
        // Arrange
        _socket.SetupGet(x => x.CloseStatusDescription)
            .Returns(string.Empty).Verifiable();

        // Act
        var actual = _proxy.CloseStatusDescription;

        // Assert
        Assert.NotNull(actual);
        _socket.VerifyGet(x => x.CloseStatusDescription, Times.Once());
    }

    [Fact]
    public void State_Proxied()
    {
        // Arrange
        _socket.SetupGet(x => x.State)
            .Returns(WebSocketState.None).Verifiable();

        // Act
        var actual = _proxy.State;

        // Assert
        Assert.Equal(WebSocketState.None, actual);
        _socket.VerifyGet(x => x.State, Times.Once());
    }

    [Fact]
    public void SubProtocol_Proxied()
    {
        // Arrange
        _socket.SetupGet(x => x.SubProtocol)
            .Returns(Uri.UriSchemeWss).Verifiable();

        // Act
        var actual = _proxy.SubProtocol;

        // Assert
        Assert.Equal(Uri.UriSchemeWss, actual);
        _socket.VerifyGet(x => x.SubProtocol, Times.Once());
    }

    [Fact]
    public void Abort_Proxied()
    {
        // Arrange
        _socket.Setup(x => x.Abort()).Verifiable();

        // Act
        _proxy.Abort();

        // Assert
        _socket.Verify(x => x.Abort(), Times.Once());
    }

    [Fact]
    public async Task CloseAsync_Proxied()
    {
        // Arrange
        _socket.Setup(x => x.CloseAsync(It.IsAny<WebSocketCloseStatus>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Verifiable();

        // Act
        await _proxy.CloseAsync(WebSocketCloseStatus.Empty, string.Empty, CancellationToken.None);

        // Assert
        _socket.Verify(
            x => x.CloseAsync(It.IsAny<WebSocketCloseStatus>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task CloseOutputAsync_Proxied()
    {
        // Arrange
        _socket.Setup(x => x.CloseOutputAsync(It.IsAny<WebSocketCloseStatus>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Verifiable();

        // Act
        await _proxy.CloseOutputAsync(WebSocketCloseStatus.Empty, string.Empty, CancellationToken.None);

        // Assert
        _socket.Verify(
            x => x.CloseOutputAsync(It.IsAny<WebSocketCloseStatus>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task ReceiveAsync_Proxied()
    {
        // Arrange
        var expected = new WebSocketReceiveResult(123, WebSocketMessageType.Binary, true);
        _socket.Setup(x => x.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected).Verifiable();

        // Act
        var actual = await _proxy.ReceiveAsync(ArraySegment<byte>.Empty, CancellationToken.None);

        // Assert
        Assert.Equal(expected, actual);
        _socket.Verify(
            x => x.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task SendAsync_Proxied()
    {
        // Arrange
        var expected = new WebSocketReceiveResult(123, WebSocketMessageType.Binary, true);
        _socket.Setup(x => x.SendAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<WebSocketMessageType>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask).Verifiable();

        // Act
        await _proxy.SendAsync(ArraySegment<byte>.Empty, WebSocketMessageType.Binary, true, CancellationToken.None);

        // Assert
        _socket.Verify(
            x => x.SendAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<WebSocketMessageType>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Once());
    }
}
