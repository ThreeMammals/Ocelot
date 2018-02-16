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
            if (reRoute.DownstreamReRoute.UseServiceDiscovery)
            {
                return GetServiceDiscoveryProvider(reRoute.DownstreamReRoute.ServiceName, serviceConfig.ServiceProviderHost, serviceConfig.ServiceProviderPort);
            }

            var services = new List<Service>();

            foreach (var downstreamAddress in reRoute.DownstreamReRoute.DownstreamAddresses)
            {
                var service = new Service(reRoute.DownstreamReRoute.ServiceName, new ServiceHostAndPort(downstreamAddress.Host, downstreamAddress.Port), string.Empty, string.Empty, new string[0]);
                
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
