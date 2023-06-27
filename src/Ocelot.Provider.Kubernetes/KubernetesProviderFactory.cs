using System;

using KubeClient;

using Microsoft.Extensions.DependencyInjection;

using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.ServiceDiscovery;

namespace Ocelot.Provider.Kubernetes
{
    public static class KubernetesProviderFactory
    {
        public static ServiceDiscoveryFinderDelegate Get { get; } = CreateProvider;
        private const string PollKube = "pollkube";

        private static ServiceDiscovery.Providers.IServiceDiscoveryProvider CreateProvider(IServiceProvider provider, ServiceProviderConfiguration config, DownstreamRoute route)
        {
            var factory = provider.GetService<IOcelotLoggerFactory>();
            var kubeClient = provider.GetService<IKubeApiClient>();

            var k8SRegistryConfiguration = new KubeRegistryConfiguration
            {
                KeyOfServiceInK8s = route.ServiceName,
                KubeNamespace = string.IsNullOrEmpty(route.ServiceNamespace) ? config.Namespace : route.ServiceNamespace,
            };

            var k8SServiceDiscoveryProvider = new KubernetesServiceDiscoveryProvider(k8SRegistryConfiguration, factory, kubeClient);

            if (config.Type?.ToLower() == PollKube)
            {
                return new PollKubernetes(config.PollingInterval, factory, k8SServiceDiscoveryProvider);
            }

            return k8SServiceDiscoveryProvider;
        }
    }
}
