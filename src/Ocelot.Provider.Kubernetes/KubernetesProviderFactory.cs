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
            var kubeClientFactory = provider.GetService<IKubeApiClientFactory>();
            var k8sRegistryConfiguration = new KubeRegistryConfiguration()
            {
                ApiEndPoint = new Uri($"https://{config.Host}:{config.Port}"),
                KeyOfServiceInK8s = name,
                KubeNamespace = config.Namespace,
                AuthStrategy = KubeAuthStrategy.BearerToken,
                AccessToken = config.Token,
                AllowInsecure = true // Don't validate server certificate
            };

            var k8sServiceDiscoveryProvider = new Kube(k8sRegistryConfiguration, factory, kubeClientFactory);
            if (config.Type?.ToLower() == "pollkube")
            {
                return new PollKube(config.PollingInterval, factory, k8sServiceDiscoveryProvider);
            }
            return k8sServiceDiscoveryProvider;
        }
    }
}
