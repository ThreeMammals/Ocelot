using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Provider.Consul.Interfaces;
using Ocelot.ServiceDiscovery.Providers;

namespace Ocelot.Provider.Consul;

/// <summary>
/// TODO It must be refactored converting to real factory-class and add to DI.
/// </summary>
/// <remarks>
/// Must inherit from <see cref="IServiceDiscoveryProviderFactory"/> interface.
/// Also the <see cref="ServiceDiscoveryFinderDelegate"/> must be removed from the design.
/// </remarks>
public static class ConsulProviderFactory // TODO : IServiceDiscoveryProviderFactory
{
    /// <summary>String constant used for provider type definition.</summary>
    public const string PollConsul = nameof(Provider.Consul.PollConsul);

    private static readonly List<PollConsul> ServiceDiscoveryProviders = new(); // TODO It must be scoped service in DI-container
    private static readonly object SyncRoot = new();

    public static ServiceDiscoveryFinderDelegate Get { get; } = CreateProvider;
    private static IServiceDiscoveryProvider CreateProvider(IServiceProvider provider, ServiceProviderConfiguration config, DownstreamRoute route)
    {
        // Singleton services
        var factory = provider.GetService<IOcelotLoggerFactory>();
        var consulFactory = provider.GetService<IConsulClientFactory>();
        var contextAccessor = provider.GetService<IHttpContextAccessor>();

        // Scoped services
        var context = contextAccessor.HttpContext;
        var configuration = new ConsulRegistryConfiguration(config.Scheme, config.Host, config.Port, route.ServiceName, config.Token); // TODO Why not to pass 2 args only: config, route? LoL
        context.Items[nameof(ConsulRegistryConfiguration)] = configuration; // initialize data
        var serviceBuilder = context.RequestServices.GetService<IConsulServiceBuilder>(); // consume data in default/custom builder

        var consulProvider = new Consul(configuration, factory, consulFactory, serviceBuilder); // TODO It must be added to DI-container!

        if (PollConsul.Equals(config.Type, StringComparison.OrdinalIgnoreCase))
        {
            lock (SyncRoot)
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
