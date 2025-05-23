using System.Net.WebSockets;

namespace Ocelot.WebSockets;

internal static class WebSocketExtensions
{
    public static Task TryCloseOutputAsync(this WebSocket webSocket, WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellation)
        => (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseReceived)
            ? webSocket.CloseOutputAsync(closeStatus, statusDescription, cancellation)
            : Task.CompletedTask;
}
