using System.Collections.Generic;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public interface IAggregatesCreator
    {
        List<ReRoute> Create(FileConfiguration fileConfiguration, List<ReRoute> reRoutes);
    }
}
