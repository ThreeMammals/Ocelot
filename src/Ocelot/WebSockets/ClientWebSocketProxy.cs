using System.Net.WebSockets;

namespace Ocelot.WebSockets;

public class ClientWebSocketProxy : WebSocket, IClientWebSocket
{
    // RealSubject (Service) class of Proxy design pattern
    private readonly ClientWebSocket _realService;
    private readonly IClientWebSocketOptions _options;

    public ClientWebSocketProxy()
    {
        _realService = new ClientWebSocket();
        _options = new ClientWebSocketOptionsProxy(_realService.Options);
    }

    // ClientWebSocket implementations
    public IClientWebSocketOptions Options => _options;

    public Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
        => _realService.ConnectAsync(uri, cancellationToken);

    // WebSocket implementations
    public override WebSocketCloseStatus? CloseStatus => _realService.CloseStatus;

    public override string CloseStatusDescription => _realService.CloseStatusDescription;

    public override WebSocketState State => _realService.State;

    public override string SubProtocol => _realService.SubProtocol;

    public override void Abort() => _realService.Abort();

    public override Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        => _realService.CloseAsync(closeStatus, statusDescription, cancellationToken);

    public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        => _realService.CloseOutputAsync(closeStatus, statusDescription, cancellationToken);

    public override void Dispose() => _realService.Dispose();

    public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        => _realService.ReceiveAsync(buffer, cancellationToken);

    public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        => _realService.SendAsync(buffer, messageType, endOfMessage, cancellationToken);

    public WebSocket ToWebSocket() => _realService;
}
