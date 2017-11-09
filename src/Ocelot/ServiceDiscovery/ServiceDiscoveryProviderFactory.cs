using System.Collections.Generic;
using Ocelot.Configuration;
using Ocelot.Values;

namespace Ocelot.ServiceDiscovery
{
    public class ServiceDiscoveryProviderFactory : IServiceDiscoveryProviderFactory
    {
        public  IServiceDiscoveryProvider Get(ServiceProviderConfiguration serviceConfig, ReRoute reRoute)
        {
            if (reRoute.UseServiceDiscovery)
            {
                return GetServiceDiscoveryProvider(reRoute.ServiceName, serviceConfig.ServiceProviderHost, serviceConfig.ServiceProviderPort);
            }

            var services = new List<Service>()
            {
                new Service(reRoute.ServiceName, 
                new HostAndPort(reRoute.DownstreamHost, reRoute.DownstreamPort),
                string.Empty, 
                string.Empty, 
                new string[0])
            };

            return new ConfigurationServiceProvider(services);
        }

        private IServiceDiscoveryProvider GetServiceDiscoveryProvider(string keyOfServiceInConsul, string providerHostName, int providerPort)
        {
            var consulRegistryConfiguration = new ConsulRegistryConfiguration(providerHostName, providerPort, keyOfServiceInConsul);
            return new ConsulServiceDiscoveryProvider(consulRegistryConfiguration);
        }
    }
}