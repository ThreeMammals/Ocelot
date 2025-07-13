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
        if (client == null)
        {
            throw new NullReferenceException($"Cannot get an {nameof(IDiscoveryClient)} service during {nameof(CreateProvider)} operation to instanciate the {nameof(Eureka)} provider!");
        }

        return Eureka.Equals(config.Type, StringComparison.OrdinalIgnoreCase)
            ? new Eureka(route.ServiceName, client)
            : null;
    }
}
