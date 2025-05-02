using System.Net.WebSockets;

namespace Ocelot.WebSockets;

internal static class WebSocketExtensions
{
    public static async Task<bool> TryCloseOutputAsync(this WebSocket webSocket, WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellation)
    {
        if (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseReceived)
        {
            await webSocket.CloseOutputAsync(closeStatus, statusDescription, cancellation);
            return true;
        }

        return false;
    }
}
