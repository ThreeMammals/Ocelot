using System.Net.WebSockets;

namespace Ocelot.WebSockets;

public class WebSocketsFactory : IWebSocketsFactory
{
    public IClientWebSocket CreateClient()
    {
        var socket = new ClientWebSocket();
        var connector = new ClientWebSocketConnector(socket);
        return new ClientWebSocketProxy(socket, connector);
    }
}
