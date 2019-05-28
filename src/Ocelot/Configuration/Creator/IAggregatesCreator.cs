using Ocelot.Configuration.File;
using System.Collections.Generic;

namespace Ocelot.Configuration.Creator
{
    public interface IAggregatesCreator
    {
        List<ReRoute> Create(FileConfiguration fileConfiguration, List<ReRoute> reRoutes);
    }
}
