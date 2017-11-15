using System.Collections.Generic;

namespace Ocelot.Configuration
{
    public class OcelotConfiguration : IOcelotConfiguration
    {
        public OcelotConfiguration(List<ReRoute> reRoutes, string administrationPath, ServiceProviderConfiguration serviceProviderConfiguration)
        {
            ReRoutes = reRoutes;
            AdministrationPath = administrationPath;
            ServiceProviderConfiguration = serviceProviderConfiguration;
        }

        public List<ReRoute> ReRoutes { get; }
        public string AdministrationPath {get;}
        public ServiceProviderConfiguration ServiceProviderConfiguration {get;}
    }
}