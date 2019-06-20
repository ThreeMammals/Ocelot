using KubeClient;
using KubeClient.Models;
using Ocelot.Logging;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ocelot.Provider.Kubernetes
{
    public class Kube : IServiceDiscoveryProvider
    {
        private KubeRegistryConfiguration kubeRegistryConfiguration;
        private IOcelotLogger logger;
        private IKubeApiClient kubeApi;

        public Kube(KubeRegistryConfiguration kubeRegistryConfiguration, IOcelotLoggerFactory factory, IKubeApiClient kubeApi)
        {
            this.kubeRegistryConfiguration = kubeRegistryConfiguration;
            this.logger = factory.CreateLogger<Kube>();
            this.kubeApi = kubeApi;
        }

        public async Task<List<Service>> Get()
        {
            var service = await kubeApi.ServicesV1().Get(kubeRegistryConfiguration.KeyOfServiceInK8s, kubeRegistryConfiguration.KubeNamespace);
            var services = new List<Service>();
            if (IsValid(service))
            {
                services.Add(BuildService(service));
            }
            else
            {
                logger.LogWarning($"namespace:{kubeRegistryConfiguration.KubeNamespace }service:{kubeRegistryConfiguration.KeyOfServiceInK8s} Unable to use ,it is invalid. Address must contain host only e.g. localhost and port must be greater than 0");
            }
            return services;
        }

        private bool IsValid(ServiceV1 service)
        {
            if (string.IsNullOrEmpty(service.Spec.ClusterIP) || service.Spec.Ports.Count <= 0)
            {
                return false;
            }

            return true;
        }

        private Service BuildService(ServiceV1 serviceEntry)
        {
            var servicePort = serviceEntry.Spec.Ports.FirstOrDefault();
            return new Service(
                serviceEntry.Metadata.Name,
                new ServiceHostAndPort(serviceEntry.Spec.ClusterIP, servicePort.Port),
                serviceEntry.Metadata.Uid,
                string.Empty,
                Enumerable.Empty<string>());
        }
    }
}
