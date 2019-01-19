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
            if (config.Type?.ToLower() == "k8s")
            {
                return GetkubeProvider(provider, config, name, factory);
            }
            return null;
        };

        private static ServiceDiscovery.Providers.IServiceDiscoveryProvider GetkubeProvider(IServiceProvider provider, Configuration.ServiceProviderConfiguration config, string name, IOcelotLoggerFactory factory)
        {
            var kubeClientFactory = provider.GetService<IKubeApiClientFactory>();
            var k8sRegistryConfiguration = new KubeRegistryConfiguration()
            {
                ApiEndPoint = new Uri($"http://{config.Host}:{config.Port}"),
                KeyOfServiceInK8s = name,
                KubeNamespace = config.Namesapce,
                AuthStrategy = KubeAuthStrategy.BearerToken,
                AccessToken = config.Token,
                AllowInsecure = true // Don't validate server certificate
            };

            var k8sServiceDiscoveryProvider = new KubeProvider(k8sRegistryConfiguration, factory, kubeClientFactory);

            return k8sServiceDiscoveryProvider;
        }
    }
}
