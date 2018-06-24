using System.Collections.Generic;
using Ocelot.Configuration;
using Ocelot.Infrastructure.Consul;
using Ocelot.Logging;
using Ocelot.ServiceDiscovery.Configuration;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;

namespace Ocelot.ServiceDiscovery
{    
    using Steeltoe.Common.Discovery;

    public class ServiceDiscoveryProviderFactory : IServiceDiscoveryProviderFactory
    {
        private readonly IOcelotLoggerFactory _factory;
        private readonly IConsulClientFactory _consulFactory;
        private readonly IDiscoveryClient _eurekaClient;

        public ServiceDiscoveryProviderFactory(IOcelotLoggerFactory factory, IConsulClientFactory consulFactory, IDiscoveryClient eurekaClient)
        {
            _factory = factory;
            _consulFactory = consulFactory;
            _eurekaClient = eurekaClient;
        }

        public IServiceDiscoveryProvider Get(ServiceProviderConfiguration serviceConfig, DownstreamReRoute reRoute)
        {
            if (reRoute.UseServiceDiscovery)
            {
                return GetServiceDiscoveryProvider(serviceConfig, reRoute.ServiceName);
            }

            var services = new List<Service>();

            foreach (var downstreamAddress in reRoute.DownstreamAddresses)
            {
                var service = new Service(reRoute.ServiceName, new ServiceHostAndPort(downstreamAddress.Host, downstreamAddress.Port), string.Empty, string.Empty, new string[0]);
                
                services.Add(service);
            }

            return new ConfigurationServiceProvider(services);
        }

        private IServiceDiscoveryProvider GetServiceDiscoveryProvider(ServiceProviderConfiguration serviceConfig, string serviceName)
        {
            if (serviceConfig.Type?.ToLower() == "servicefabric")
            {
                var config = new ServiceFabricConfiguration(serviceConfig.Host, serviceConfig.Port, serviceName);
                return new ServiceFabricServiceDiscoveryProvider(config);
            }

            if (serviceConfig.Type?.ToLower() == "eureka")
            {
                return new EurekaServiceDiscoveryProvider(serviceName, _eurekaClient);
            }
            
            var consulRegistryConfiguration = new ConsulRegistryConfiguration(serviceConfig.Host, serviceConfig.Port, serviceName, serviceConfig.Token);

            var consulServiceDiscoveryProvider =  new ConsulServiceDiscoveryProvider(consulRegistryConfiguration, _factory, _consulFactory);

            if (serviceConfig.Type?.ToLower() == "pollconsul")
            {
                return new PollingConsulServiceDiscoveryProvider(serviceConfig.PollingInterval, consulRegistryConfiguration.KeyOfServiceInConsul, _factory, consulServiceDiscoveryProvider);
            }

            return consulServiceDiscoveryProvider;
        }
    }
}
