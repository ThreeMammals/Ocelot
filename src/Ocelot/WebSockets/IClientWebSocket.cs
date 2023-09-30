using System.Net.WebSockets;

namespace Ocelot.WebSockets;

public interface IClientWebSocket
{
    WebSocket ToWebSocket();

    // ClientWebSocket definitions
    IClientWebSocketOptions Options { get; }
    Task ConnectAsync(Uri uri, CancellationToken cancellationToken);

    // WebSocket definitions
    WebSocketCloseStatus? CloseStatus { get; }
    string CloseStatusDescription { get; }
    WebSocketState State { get; }
    string SubProtocol { get; }
    void Abort();
    Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken);
    Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken);
    void Dispose();
    Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken);
    Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken);
}
