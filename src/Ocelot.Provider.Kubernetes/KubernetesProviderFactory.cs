using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Provider.Kubernetes.Interfaces;
using System.Reactive.Concurrency;

namespace Ocelot.Provider.Kubernetes
{
    public static class KubernetesProviderFactory // TODO : IServiceDiscoveryProviderFactory
    {
        /// <summary>
        /// String constant used for provider type definition.
        /// </summary>
        public const string PollKube = nameof(Kubernetes.PollKube);

        public const string WatchKube = nameof(Kubernetes.WatchKube);

        public static ServiceDiscoveryFinderDelegate Get { get; } = CreateProvider;

        private static IServiceDiscoveryProvider CreateProvider(IServiceProvider provider, ServiceProviderConfiguration config, DownstreamRoute route)
        {
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
                return new WatchKube(configuration, factory, kubeClient, serviceBuilder, Scheduler.Default);
            }

            var defaultK8sProvider = new Kube(configuration, factory, kubeClient, serviceBuilder);
 
            return PollKube.Equals(config.Type, StringComparison.OrdinalIgnoreCase)
                ? new PollKube(config.PollingInterval, factory, defaultK8sProvider)
                : defaultK8sProvider;
        }
    }
}
