using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public interface IConfigurationCreator
    {
        InternalConfiguration Create(FileConfiguration fileConfiguration, List<Route> routes);
    }
}
