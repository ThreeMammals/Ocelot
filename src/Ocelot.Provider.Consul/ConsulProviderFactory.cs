using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Polling;
using Ocelot.ServiceDiscovery.Providers;

namespace Ocelot.Provider.Consul;

public static class ConsulProviderFactory
{
    /// <summary>
    ///     String constant used for provider type definition.
    /// </summary>
    public const string PollConsul = nameof(Provider.Consul.PollConsul);
    private static readonly PollingServicesManager<Consul, PollConsul> ServicesManager = new();

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
            return ServicesManager.GetServicePollingHandler(consulProvider, route.ServiceName, config.PollingInterval, factory);
        }

        return consulProvider;
    }
}
