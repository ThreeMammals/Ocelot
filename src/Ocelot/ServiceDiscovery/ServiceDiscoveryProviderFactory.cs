using System.Collections.Generic;
using Ocelot.Values;

namespace Ocelot.ServiceDiscovery
{
    public class ServiceDiscoveryProviderFactory : IServiceDiscoveryProviderFactory
    {
        public  IServiceDiscoveryProvider Get(ServiceProviderConfiguraion serviceConfig)
        {
            var services = new List<Service>()
            {
                new Service(serviceConfig.ServiceName, new HostAndPort(serviceConfig.DownstreamHost, serviceConfig.DownstreamPort))
            };

            return new ConfigurationServiceProvider(services);
        }
    }
}