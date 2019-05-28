using Ocelot.Configuration.File;
using System.Collections.Generic;

namespace Ocelot.Configuration.Creator
{
    public interface IDynamicsCreator
    {
        List<ReRoute> Create(FileConfiguration fileConfiguration);
    }
}
