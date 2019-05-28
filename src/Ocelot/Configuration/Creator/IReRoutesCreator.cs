using Ocelot.Configuration.File;
using System.Collections.Generic;

namespace Ocelot.Configuration.Creator
{
    public interface IReRoutesCreator
    {
        List<ReRoute> Create(FileConfiguration fileConfiguration);
    }
}
