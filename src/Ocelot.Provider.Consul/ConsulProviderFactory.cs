using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.ServiceDiscovery.Providers;

namespace Ocelot.Provider.Consul;

public static class ConsulProviderFactory
{
    /// <summary>
    /// String constant used for provider type definition.
    /// </summary>
    public const string PollConsul = nameof(Provider.Consul.PollConsul);

    private static readonly List<PollConsul> ServiceDiscoveryProviders = new();
    private static readonly object LockObject = new();

    public static ServiceDiscoveryFinderDelegate Get { get; } = CreateProvider;

    private static IServiceDiscoveryProvider CreateProvider(IServiceProvider provider,
        ServiceProviderConfiguration config, DownstreamRoute route)
    {
        var factory = provider.GetService<IOcelotLoggerFactory>();
        var consulFactory = provider.GetService<IConsulClientFactory>();

        var consulRegistryConfiguration = new ConsulRegistryConfiguration(
            config.Scheme, config.Host, config.Port, route.ServiceName, config.Token);

        var consulProvider = new Consul(consulRegistryConfiguration, factory, consulFactory);

        if (PollConsul.Equals(config.Type, StringComparison.OrdinalIgnoreCase))
        {
            lock (LockObject)
            {
                var discoveryProvider = ServiceDiscoveryProviders.FirstOrDefault(x => x.ServiceName == route.ServiceName);
                if (discoveryProvider != null)
                {
                    return discoveryProvider;
                }

                discoveryProvider = new PollConsul(config.PollingInterval, route.ServiceName, factory, consulProvider);
                ServiceDiscoveryProviders.Add(discoveryProvider);
                return discoveryProvider;
            }
        }

        return consulProvider;
    }
}
