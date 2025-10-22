using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public interface IConfigurationCreator
{
    InternalConfiguration Create(FileConfiguration configuration, List<Route> routes);
}
