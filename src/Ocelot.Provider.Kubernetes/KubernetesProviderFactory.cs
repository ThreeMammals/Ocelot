using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Provider.Kubernetes.Interfaces;

namespace Ocelot.Provider.Kubernetes
{
    public static class KubernetesProviderFactory
    {
        /// <summary>
        /// String constant used for provider type definition.
        /// </summary>
        public const string PollKube = nameof(Kubernetes.PollKube);

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

            var defaultK8sProvider = new Kube(configuration, factory, kubeClient, serviceBuilder);
 
            return PollKube.Equals(config.Type, StringComparison.OrdinalIgnoreCase)
                ? new PollKube(config.PollingInterval, factory, defaultK8sProvider)
                : defaultK8sProvider;
        }
    }
}
