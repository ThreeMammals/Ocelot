using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public interface IDynamicsCreator
    {
        List<Route> Create(FileConfiguration fileConfiguration);
    }
}
