using Ocelot.WebSockets;
using System.Net.WebSockets;

namespace Ocelot.UnitTests.WebSockets;

public class WebSocketExtensionsTests
{
    private readonly Mock<WebSocket> _webSocket = new();

    [Fact]
    [Trait("Bug", "930")]
    [Trait("PR", "2091")] // https://github.com/ThreeMammals/Ocelot/pull/2091
    public async Task TryCloseOutputAsync_NoState_NoClosing()
    {
        // Arrange
        _webSocket.SetupGet(x => x.State)
            .Returns(WebSocketState.None);
        _webSocket.Setup(x => x.CloseOutputAsync(It.IsAny<WebSocketCloseStatus>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask).Verifiable();

        // Act
        var actual = _webSocket.Object.TryCloseOutputAsync(WebSocketCloseStatus.Empty, string.Empty, CancellationToken.None);
        await actual;

        // Assert
        Assert.True(actual.IsCompleted);
        Assert.Equal(Task.CompletedTask, actual);
        _webSocket.Verify(
            x => x.CloseOutputAsync(It.IsAny<WebSocketCloseStatus>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }

    [Theory]
    [Trait("Bug", "930")]
    [Trait("PR", "2091")] // https://github.com/ThreeMammals/Ocelot/pull/2091
    [InlineData(WebSocketState.Open)]
    [InlineData(WebSocketState.CloseReceived)]
    public async Task TryCloseOutputAsync_MatchingState_HappyPath(WebSocketState state)
    {
        bool closed = false;
        Task Closing()
        {
            closed = true;
            return Task.CompletedTask;
        }

        // Arrange
        _webSocket.SetupGet(x => x.State)
            .Returns(state);
        _webSocket.Setup(x => x.CloseOutputAsync(It.IsAny<WebSocketCloseStatus>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Closing).Verifiable();

        // Act
        var actual = _webSocket.Object.TryCloseOutputAsync(WebSocketCloseStatus.Empty, string.Empty, CancellationToken.None);
        await actual;

        // Assert
        Assert.True(actual.IsCompleted);
        Assert.Equal(Task.CompletedTask, actual);
        _webSocket.Verify(
            x => x.CloseOutputAsync(It.IsAny<WebSocketCloseStatus>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once());
        Assert.True(closed);
    }
}
