using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public interface IHttpHandlerOptionsCreator
{
    HttpHandlerOptions Create(FileHttpHandlerOptions options);
    HttpHandlerOptions Create(FileRoute route, FileGlobalConfiguration globalConfiguration);
    HttpHandlerOptions Create(FileDynamicRoute route, FileGlobalConfiguration globalConfiguration);
}
