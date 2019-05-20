using KubeClient;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Logging;
using Ocelot.ServiceDiscovery;
using System;

namespace Ocelot.Provider.Kubernetes
{
    public static class KubernetesProviderFactory
    {
        public static ServiceDiscoveryFinderDelegate Get = (provider, config, name) =>
        {
            var factory = provider.GetService<IOcelotLoggerFactory>();
            return GetkubeProvider(provider, config, name, factory);
        };

        private static ServiceDiscovery.Providers.IServiceDiscoveryProvider GetkubeProvider(IServiceProvider provider, Configuration.ServiceProviderConfiguration config, string name, IOcelotLoggerFactory factory)
        {
            var kubeClient = provider.GetService<IKubeApiClient>();
            var k8sRegistryConfiguration = new KubeRegistryConfiguration()
            {
                KeyOfServiceInK8s = name,
                KubeNamespace = config.Namespace,
            };

            var k8sServiceDiscoveryProvider = new Kube(k8sRegistryConfiguration, factory, kubeClient);
            if (config.Type?.ToLower() == "pollkube")
            {
                return new PollKube(config.PollingInterval, factory, k8sServiceDiscoveryProvider);
            }
            return k8sServiceDiscoveryProvider;
        }
    }
}
