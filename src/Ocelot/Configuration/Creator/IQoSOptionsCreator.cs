using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public interface IQoSOptionsCreator
{
    QoSOptions Create(FileQoSOptions options);

    QoSOptions Create(FileRoute route, FileGlobalConfiguration globalConfiguration);
}
