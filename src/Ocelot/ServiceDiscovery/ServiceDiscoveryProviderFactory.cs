namespace Ocelot.ServiceDiscovery
{
    using System.Collections.Generic;
    using Ocelot.Configuration;
    using Ocelot.Logging;
    using Ocelot.ServiceDiscovery.Configuration;
    using Ocelot.ServiceDiscovery.Providers;
    using Ocelot.Values;
    using System;
    using System.Linq;
    using Microsoft.Extensions.DependencyInjection;
    using Steeltoe.Common.Discovery;

    public class ServiceDiscoveryProviderFactory : IServiceDiscoveryProviderFactory
    {
        private readonly IOcelotLoggerFactory _factory;
        private readonly IDiscoveryClient _eurekaClient;
        private readonly List<ServiceDiscoveryFinderDelegate> _delegates;
        private readonly IServiceProvider _provider;

        public ServiceDiscoveryProviderFactory(IOcelotLoggerFactory factory, IDiscoveryClient eurekaClient, IServiceProvider provider)
        {
            _factory = factory;
            _eurekaClient = eurekaClient;
            _provider = provider;
            _delegates = provider
                .GetServices<ServiceDiscoveryFinderDelegate>()
                .ToList();
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

        private IServiceDiscoveryProvider GetServiceDiscoveryProvider(ServiceProviderConfiguration config, string key)
        {
            if (config.Type?.ToLower() == "servicefabric")
            {
                var sfConfig = new ServiceFabricConfiguration(config.Host, config.Port, key);
                return new ServiceFabricServiceDiscoveryProvider(sfConfig);
            }

            if (config.Type?.ToLower() == "eureka")
            {
                return new EurekaServiceDiscoveryProvider(key, _eurekaClient);
            }

            foreach (var serviceDiscoveryFinderDelegate in _delegates)
            {
                var provider = serviceDiscoveryFinderDelegate?.Invoke(_provider, config, key);
                if (provider != null)
                {
                    return provider;
                }
            }

            return null;
        }
    }
}
