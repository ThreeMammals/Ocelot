using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public interface IAggregatesCreator
{
    IEnumerable<Route> Create(FileConfiguration fileConfiguration, IReadOnlyList<Route> routes);
}
