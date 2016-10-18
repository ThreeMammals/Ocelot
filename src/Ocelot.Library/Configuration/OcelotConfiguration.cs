using System.Collections.Generic;

namespace Ocelot.Library.Configuration
{
    public class OcelotConfiguration : IOcelotConfiguration
    {
        public OcelotConfiguration(List<ReRoute> reRoutes)
        {
            ReRoutes = reRoutes;
        }

        public List<ReRoute> ReRoutes { get; }
    }
}