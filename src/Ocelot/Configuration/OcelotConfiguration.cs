using System.Collections.Generic;

namespace Ocelot.Configuration
{
    public class OcelotConfiguration : IOcelotConfiguration
    {
        public OcelotConfiguration(List<ReRoute> reRoutes, string administrationPath, ServiceProviderConfiguration serviceProviderConfiguration, string requestId)
        {
            ReRoutes = reRoutes;
            AdministrationPath = administrationPath;
            ServiceProviderConfiguration = serviceProviderConfiguration;
            RequestId = requestId;
        }

        public List<ReRoute> ReRoutes { get; }
        public string AdministrationPath {get;}
        public ServiceProviderConfiguration ServiceProviderConfiguration {get;}
        public string RequestId {get;}
    }
}