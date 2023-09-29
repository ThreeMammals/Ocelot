using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Polling;

namespace Ocelot.Provider.Kubernetes;

public static class KubernetesProviderFactory
{
    /// <summary>
    ///     String constant used for provider type definition.
    /// </summary>
    public const string PollKube = nameof(Kubernetes.PollKube);

    private static readonly PollingServicesManager<KubernetesServiceDiscoveryProvider, PollKube>
        ServicesManager = new();

    public static ServiceDiscoveryFinderDelegate Get { get; } = CreateProvider;

    private static IServiceDiscoveryProvider CreateProvider(IServiceProvider provider,
        ServiceProviderConfiguration config, DownstreamRoute route)
    {
        var factory = provider.GetService<IOcelotLoggerFactory>();
        var kubeClient = provider.GetService<IKubeApiClient>();

        var k8SRegistryConfiguration = new KubeRegistryConfiguration
        {
            KeyOfServiceInK8s = route.ServiceName,
            KubeNamespace = string.IsNullOrEmpty(route.ServiceNamespace) ? config.Namespace : route.ServiceNamespace,
        };

        var k8SServiceDiscoveryProvider =
            new KubernetesServiceDiscoveryProvider(k8SRegistryConfiguration, factory, kubeClient);

        if (PollKube.Equals(config.Type, StringComparison.OrdinalIgnoreCase))
        {
            return ServicesManager.GetServicePollingHandler(k8SServiceDiscoveryProvider, route.ServiceName,
                config.PollingInterval, factory);

        }

        return k8SServiceDiscoveryProvider;
    }
}
