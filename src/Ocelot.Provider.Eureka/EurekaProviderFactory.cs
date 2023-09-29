using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Polling;
using Ocelot.ServiceDiscovery.Providers;

namespace Ocelot.Provider.Eureka;

public static class EurekaProviderFactory
{
    /// <summary>
    /// String constant used for provider type definition.
    /// </summary>
    public const string PollEureka = nameof(Provider.Eureka.PollEureka);
    private static readonly PollingServicesManager<Eureka, PollEureka> ServicesManager = new();

    public static ServiceDiscoveryFinderDelegate Get { get; } = CreateProvider;

    private static IServiceDiscoveryProvider CreateProvider(IServiceProvider provider, ServiceProviderConfiguration config, DownstreamRoute route)
    {
        var factory = provider.GetService<IOcelotLoggerFactory>();
        var client = provider.GetService<IDiscoveryClient>();

        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        var eurekaProvider = new Eureka(route.ServiceName, client);

        if (PollEureka.Equals(config.Type, StringComparison.OrdinalIgnoreCase))
        {
            return ServicesManager.GetServicePollingHandler(eurekaProvider, route.ServiceName, config.PollingInterval, factory);
        }

        return eurekaProvider;
    }
}
