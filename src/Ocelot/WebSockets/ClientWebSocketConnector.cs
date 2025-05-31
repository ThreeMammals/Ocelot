using System.Net.WebSockets;

namespace Ocelot.WebSockets;

public class ClientWebSocketConnector : IClientWebSocketConnector
{
    private readonly ClientWebSocket _webSocket;
    private readonly IClientWebSocketOptions _options;

    public ClientWebSocketConnector(ClientWebSocket webSocket)
    {
        _webSocket = webSocket;
        _options = new ClientWebSocketOptionsProxy(webSocket.Options);
    }

    public WebSocket ToWebSocket() => _webSocket;

    public IClientWebSocketOptions Options => _options;

    public Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
        => _webSocket.ConnectAsync(uri, cancellationToken);
}
