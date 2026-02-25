using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Provider.Kubernetes.Interfaces;
//using System.Collections.Concurrent;
using System.Reactive.Concurrency;

namespace Ocelot.Provider.Kubernetes;

public static class KubernetesProviderFactory // TODO : IServiceDiscoveryProviderFactory
{
    /// <summary>String constant used for provider type definition.</summary>
    public const string PollKube = nameof(Kubernetes.PollKube);
    public const string WatchKube = nameof(Kubernetes.WatchKube);

    // private static readonly ConcurrentDictionary<string, IServiceDiscoveryProvider> _providers = new(); // TODO It must be singleton service in DI-container
    public static ServiceDiscoveryFinderDelegate Get { get; } = CreateProvider;

    private static IServiceDiscoveryProvider CreateProvider(IServiceProvider provider, ServiceProviderConfiguration config, DownstreamRoute route)
    {
        //if (_providers.TryGetValue(route.LoadBalancerKey, out var instance)) // ?? route.ServiceName ??
        //    return instance;
        var factory = provider.GetService<IOcelotLoggerFactory>();
        var kubeClient = provider.GetService<IKubeApiClient>();
        var serviceBuilder = provider.GetService<IKubeServiceBuilder>();
        var configuration = new KubeRegistryConfiguration
        {
            KeyOfServiceInK8s = route.ServiceName,
            KubeNamespace = string.IsNullOrEmpty(route.ServiceNamespace) ? config.Namespace : route.ServiceNamespace,
            Scheme = route.DownstreamScheme,
        };

        if (WatchKube.Equals(config.Type, StringComparison.OrdinalIgnoreCase))
        {
            //return _providers.GetOrAdd(route.LoadBalancerKey,
            //    key => new WatchKube(configuration, factory, kubeClient, serviceBuilder, Scheduler.Default));
            return new WatchKube(configuration, factory, kubeClient, serviceBuilder, Scheduler.Default);
        }

        var kubeProvider = new Kube(configuration, factory, kubeClient, serviceBuilder);
        return /*_providers.GetOrAdd(route.LoadBalancerKey,
            key =>*/ PollKube.Equals(config.Type, StringComparison.OrdinalIgnoreCase)
                ? new PollKube(config.PollingInterval, factory, kubeProvider)
                : kubeProvider; //);
    }
}
