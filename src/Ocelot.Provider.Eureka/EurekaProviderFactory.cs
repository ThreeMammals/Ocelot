using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.ServiceDiscovery.Providers;

namespace Ocelot.Provider.Eureka;

public static class EurekaProviderFactory
{
    /// <summary>
    /// String constant used for provider type definition.
    /// </summary>
    public const string Eureka = nameof(Provider.Eureka.Eureka);

    public static ServiceDiscoveryFinderDelegate Get { get; } = CreateProvider;

    private static IServiceDiscoveryProvider CreateProvider(IServiceProvider provider, ServiceProviderConfiguration config, DownstreamRoute route)
    {
        var client = provider.GetService<IDiscoveryClient>();

        return Eureka.Equals(config.Type, StringComparison.OrdinalIgnoreCase) && client != null
            ? new Eureka(route.ServiceName, client)
            : null;
    }
}
