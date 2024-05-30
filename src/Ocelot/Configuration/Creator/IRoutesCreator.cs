using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public interface IRoutesCreator
{
    List<Route> Create(FileConfiguration fileConfiguration);
    
    int CreateTimeout(FileRoute fileRoute, FileGlobalConfiguration globalConfiguration);
}
