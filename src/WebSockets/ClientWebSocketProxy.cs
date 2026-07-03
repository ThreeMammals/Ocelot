using System.Net.WebSockets;

namespace Ocelot.WebSockets;

public sealed class ClientWebSocketProxy : WebSocket, IClientWebSocket
{
    // RealSubject (Service) class of Proxy design pattern
    private readonly WebSocket _realSocket;
    private readonly IClientWebSocketConnector _connector;

    public ClientWebSocketProxy(WebSocket socket, IClientWebSocketConnector connector)
    {
        _realSocket = socket;
        _connector = connector;
    }

    // IClientWebSocketConnector implementations
    public WebSocket ToWebSocket() => _realSocket;
    public IClientWebSocketOptions Options => _connector.Options;
    public Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
        => _connector.ConnectAsync(uri, cancellationToken);

    // WebSocket implementations
    public override WebSocketCloseStatus? CloseStatus => _realSocket.CloseStatus;

    public override string CloseStatusDescription => _realSocket.CloseStatusDescription;

    public override WebSocketState State => _realSocket.State;

    public override string SubProtocol => _realSocket.SubProtocol;

    public override void Abort() => _realSocket.Abort();

    public override Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        => _realSocket.CloseAsync(closeStatus, statusDescription, cancellationToken);

    public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        => _realSocket.CloseOutputAsync(closeStatus, statusDescription, cancellationToken);

    public override void Dispose() => _realSocket.Dispose();

    public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        => _realSocket.ReceiveAsync(buffer, cancellationToken);

    public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        => _realSocket.SendAsync(buffer, messageType, endOfMessage, cancellationToken);
}
