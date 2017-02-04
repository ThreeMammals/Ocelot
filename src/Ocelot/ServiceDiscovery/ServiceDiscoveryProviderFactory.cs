using System.Collections.Generic;
using Ocelot.Configuration;
using Ocelot.Values;

namespace Ocelot.ServiceDiscovery
{
    public class ServiceDiscoveryProviderFactory : IServiceDiscoveryProviderFactory
    {
        public  IServiceDiscoveryProvider Get(ServiceProviderConfiguraion serviceConfig)
        {
            if (serviceConfig.UseServiceDiscovery)
            {
                return GetServiceDiscoveryProvider(serviceConfig.ServiceName, serviceConfig.ServiceDiscoveryProvider, serviceConfig.ServiceProviderHost, serviceConfig.ServiceProviderPort);
            }

            var services = new List<Service>()
            {
                new Service(serviceConfig.ServiceName, 
                new HostAndPort(serviceConfig.DownstreamHost, serviceConfig.DownstreamPort),
                string.Empty, 
                string.Empty, 
                new string[0])
            };

            return new ConfigurationServiceProvider(services);
        }

        private IServiceDiscoveryProvider GetServiceDiscoveryProvider(string serviceName, string serviceProviderName, string providerHostName, int providerPort)
        {
            var consulRegistryConfiguration = new ConsulRegistryConfiguration(providerHostName, providerPort, serviceName);
            return new ConsulServiceDiscoveryProvider(consulRegistryConfiguration);
        }
    }
}