using System.Collections.Generic;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public interface IAggregatesCreator
    {
        List<ReRoute> Aggregates(FileConfiguration fileConfiguration, List<ReRoute> reRoutes);
    }
}
