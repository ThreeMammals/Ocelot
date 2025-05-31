using System.Net.WebSockets;

namespace Ocelot.WebSockets;

public static class WebSocketExtensions
{
    /// <summary>
    /// Closes the WebSocket only if its state is <see cref="WebSocketState.Open"/> or <see cref="WebSocketState.CloseReceived"/>.
    /// </summary>
    /// <returns>The underlying closing task if the <paramref name="webSocket"/> <see cref="WebSocket.State"/> matches; otherwise, the <see cref="Task.CompletedTask"/>.</returns>
    public static Task TryCloseOutputAsync(this WebSocket webSocket, WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellation)
        => (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseReceived)
            ? webSocket.CloseOutputAsync(closeStatus, statusDescription, cancellation)
            : Task.CompletedTask;
}
