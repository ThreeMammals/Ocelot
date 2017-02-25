using System.Collections.Generic;

namespace Ocelot.Configuration
{
    public class OcelotConfiguration : IOcelotConfiguration
    {
        public OcelotConfiguration(List<ReRoute> reRoutes, string administrationPath)
        {
            ReRoutes = reRoutes;
            AdministrationPath = administrationPath;
        }

        public List<ReRoute> ReRoutes { get; }
        public string AdministrationPath {get;}
    }
}