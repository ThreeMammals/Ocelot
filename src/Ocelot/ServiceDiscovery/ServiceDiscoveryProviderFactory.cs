using System.Collections.Generic;
using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Values;

namespace Ocelot.ServiceDiscovery
{
    public class ServiceDiscoveryProviderFactory : IServiceDiscoveryProviderFactory
    {
        private readonly IOcelotLoggerFactory _factory;

        public ServiceDiscoveryProviderFactory(IOcelotLoggerFactory factory)
        {
            _factory = factory;
        }

        public  IServiceDiscoveryProvider Get(ServiceProviderConfiguration serviceConfig, ReRoute reRoute)
        {
            if (reRoute.UseServiceDiscovery)
            {
                return GetServiceDiscoveryProvider(reRoute.ServiceName, serviceConfig.ServiceProviderHost, serviceConfig.ServiceProviderPort);
            }

            var services = new List<Service>();

            foreach (var downstreamAddress in reRoute.DownstreamAddresses)
            {
                var service = new Service(reRoute.ServiceName, new ServiceHostAndPort(downstreamAddress.Host, downstreamAddress.Port), string.Empty, string.Empty, new string[0]);
                
                services.Add(service);
            }

            return new ConfigurationServiceProvider(services);
        }

        private IServiceDiscoveryProvider GetServiceDiscoveryProvider(string keyOfServiceInConsul, string providerHostName, int providerPort)
        {
            var consulRegistryConfiguration = new ConsulRegistryConfiguration(providerHostName, providerPort, keyOfServiceInConsul);
            return new ConsulServiceDiscoveryProvider(consulRegistryConfiguration, _factory);
        }
    }
}
