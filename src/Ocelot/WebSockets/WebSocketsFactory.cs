namespace Ocelot.WebSockets;

public class WebSocketsFactory : IWebSocketsFactory
{
    public IClientWebSocket CreateClient() => new ClientWebSocketProxy();
}
