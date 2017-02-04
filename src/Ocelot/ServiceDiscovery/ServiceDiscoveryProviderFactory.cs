using System.Collections.Generic;
using Ocelot.Values;

namespace Ocelot.ServiceDiscovery
{
    public class ServiceDiscoveryProviderFactory : IServiceDiscoveryProviderFactory
    {
        public  IServiceDiscoveryProvider Get(ServiceProviderConfiguraion serviceConfig)
        {
            if (serviceConfig.UseServiceDiscovery)
            {
                return GetServiceDiscoveryProvider(serviceConfig.ServiceName, serviceConfig.ServiceDiscoveryProvider);
            }

            var services = new List<Service>()
            {
                new Service(serviceConfig.ServiceName, new HostAndPort(serviceConfig.DownstreamHost, serviceConfig.DownstreamPort))
            };

            return new ConfigurationServiceProvider(services);
        }

        private IServiceDiscoveryProvider GetServiceDiscoveryProvider(string serviceName, string serviceProviderName)
        {
            return new ConsulServiceDiscoveryProvider();
        }
    }
}