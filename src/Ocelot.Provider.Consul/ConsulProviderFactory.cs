using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Logging;
using Ocelot.ServiceDiscovery;

namespace Ocelot.Provider.Consul;

public static class ConsulProviderFactory
{
    private static readonly List<PollConsul> ServiceDiscoveryProviders = new();
    private static readonly object LockObject = new();

    public static ServiceDiscoveryFinderDelegate Get = (provider, config, route) =>
    {
        var factory = provider.GetService<IOcelotLoggerFactory>();
        var consulFactory = provider.GetService<IConsulClientFactory>();
        var consulRegistryConfiguration = new ConsulRegistryConfiguration(config.Scheme, config.Host, config.Port,
            route.ServiceName, config.Token);
        var consulServiceDiscoveryProvider = new Consul(consulRegistryConfiguration, factory, consulFactory);

        if (string.Compare(config.Type, "PollConsul", StringComparison.OrdinalIgnoreCase) != 0)
        {
            return consulServiceDiscoveryProvider;
        }

        lock (LockObject)
        {
            var discoveryProvider = ServiceDiscoveryProviders.FirstOrDefault(x => x.ServiceName == route.ServiceName);

            if (discoveryProvider != null)
            {
                return discoveryProvider;
            }

            discoveryProvider = new PollConsul(
                config.PollingInterval, route.ServiceName, factory,
                consulServiceDiscoveryProvider);

            ServiceDiscoveryProviders.Add(discoveryProvider);

            return discoveryProvider;
        }
    };
}
