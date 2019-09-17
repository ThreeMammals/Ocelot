using KubeClient;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Logging;
using Ocelot.ServiceDiscovery;
using System;
using Ocelot.Configuration;

namespace Ocelot.Provider.Kubernetes
{
    public static class KubernetesProviderFactory
    {
        public static ServiceDiscoveryFinderDelegate Get = (provider, config, reRoute) =>
        {
            var factory = provider.GetService<IOcelotLoggerFactory>();
            return GetkubeProvider(provider, config, reRoute, factory);
        };

        private static ServiceDiscovery.Providers.IServiceDiscoveryProvider GetkubeProvider(IServiceProvider provider, Configuration.ServiceProviderConfiguration config, DownstreamReRoute reRoute, IOcelotLoggerFactory factory)
        {
            var kubeClient = provider.GetService<IKubeApiClient>();
            var k8sRegistryConfiguration = new KubeRegistryConfiguration()
            {
                KeyOfServiceInK8s = reRoute.ServiceName,
                KubeNamespace = string.IsNullOrEmpty(reRoute.ServiceNamespace) ? config.Namespace : reRoute.ServiceNamespace
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
