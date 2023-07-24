using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public interface IAggregatesCreator
    {
        List<Route> Create(FileConfiguration fileConfiguration, List<Route> routes);
    }
}
