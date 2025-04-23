using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.ServiceDiscovery.Providers;
using Nacos.V2;
using Ocelot.ServiceDiscovery;

namespace Ocelot.Provider.Nacos;

public static class NacosProviderFactory
{
    /// <summary>
    /// String constant used for provider type definition.
    /// </summary>
    public const string Nacos = nameof(Provider.Nacos.Nacos);

    public static ServiceDiscoveryFinderDelegate Get { get; } = CreateProvider;

    private static IServiceDiscoveryProvider CreateProvider(IServiceProvider provider, ServiceProviderConfiguration config, DownstreamRoute route)
    {
        var client = provider.GetService<INacosNamingService>();
        if (client == null)
        {
            throw new NullReferenceException($"Cannot get an {nameof(INacosNamingService)} service during {nameof(CreateProvider)} operation to instantiate the {nameof(Nacos)} provider!");
        }

        return Nacos.Equals(config.Type, StringComparison.OrdinalIgnoreCase)
            ? new Nacos(route.ServiceName, client)
            : null;
    }
}
