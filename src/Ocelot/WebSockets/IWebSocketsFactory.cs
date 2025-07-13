namespace Ocelot.WebSockets;

public interface IWebSocketsFactory
{
    IClientWebSocket CreateClient();
}
