using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public interface IWebSocketOptionsCreator
{
    WebSocketOptions Create(FileWebSocketOptions options);
    WebSocketOptions Create(FileRoute route, FileGlobalConfiguration globalConfiguration);
    WebSocketOptions Create(FileDynamicRoute route, FileGlobalConfiguration globalConfiguration);
}
