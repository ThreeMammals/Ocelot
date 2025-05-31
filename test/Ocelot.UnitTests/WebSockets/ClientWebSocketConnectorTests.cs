using Ocelot.WebSockets;
using System.Net.Security;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;

namespace Ocelot.UnitTests.WebSockets;

public sealed class ClientWebSocketConnectorTests : UnitTest, IDisposable
{
    private readonly ClientWebSocket _injectee; // no mocking
    private readonly ClientWebSocketConnector _connector;
    public ClientWebSocketConnectorTests()
    {
        _injectee = new(); // no mocking
        _connector = new(_injectee);
    }

    public void Dispose() => _injectee.Dispose();

    [Fact]
    public void ToWebSocket_ReturnedConcrete()
    {
        // Arrange, Act
        var actual = _connector.ToWebSocket();

        // Assert
        Assert.NotNull(actual);
        Assert.IsType<ClientWebSocket>(actual);
        Assert.Equal(_injectee, actual);
    }

    [Fact]
    public void Options_ReturnedProxy()
    {
        // Arrange
        static bool RemoteCertificateValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            => true;
        _injectee.Options.RemoteCertificateValidationCallback = RemoteCertificateValidation;

        // Act
        var actual = _connector.Options;

        // Assert
        Assert.NotNull(actual);
        Assert.IsType<ClientWebSocketOptionsProxy>(actual);
        Assert.Equal(RemoteCertificateValidation, actual.RemoteCertificateValidationCallback);
    }

    [Fact]
    public async Task ConnectAsync_Proxied()
    {
        // Arrange, Act
        var url = new UriBuilder(Uri.UriSchemeWss, "echo.websocket.org");
        await _connector.ConnectAsync(url.Uri, CancellationToken.None);

        // Assert
        Assert.Equal(WebSocketState.Open, _injectee.State);
        await _injectee.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, nameof(ConnectAsync_Proxied), CancellationToken.None);
        Assert.Equal(WebSocketState.CloseSent, _injectee.State);
        await _injectee.CloseAsync(WebSocketCloseStatus.NormalClosure, nameof(ConnectAsync_Proxied), CancellationToken.None);
        Assert.Equal(WebSocketState.Closed, _injectee.State);
    }
}
