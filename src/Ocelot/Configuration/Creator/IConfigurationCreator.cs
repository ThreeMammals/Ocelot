using System.Collections.Generic;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public interface IConfigurationCreator
    {
        InternalConfiguration Create(FileConfiguration fileConfiguration, List<ReRoute> reRoutes);
    }
}
