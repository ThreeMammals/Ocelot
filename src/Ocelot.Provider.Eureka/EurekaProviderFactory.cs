using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.ServiceDiscovery;
using Ocelot.ServiceDiscovery.Providers;
using Steeltoe.Discovery;
using System;

namespace Ocelot.Provider.Eureka;

public static class EurekaProviderFactory
{
    /// <summary>
    /// String constant used for provider type definition.
    /// </summary>
    public const string Eureka = "Eureka";
    public static ServiceDiscoveryFinderDelegate Get { get; } = CreateProvider;

    private static IServiceDiscoveryProvider CreateProvider(IServiceProvider provider, ServiceProviderConfiguration config, DownstreamRoute route)
    {
        var client = provider.GetService<IDiscoveryClient>();

        if (Eureka.Equals(config.Type, StringComparison.OrdinalIgnoreCase) && client != null)
        {
            return new Eureka(route.ServiceName, client);
        }

        return null;
    }
}
