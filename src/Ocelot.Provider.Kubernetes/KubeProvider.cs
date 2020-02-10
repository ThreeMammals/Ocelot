using KubeClient;
using KubeClient.Models;
using Ocelot.Logging;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ocelot.Provider.Kubernetes.KubeApiClientExtensions;

namespace Ocelot.Provider.Kubernetes
{
    public class Kube : IServiceDiscoveryProvider
    {
        private KubeRegistryConfiguration kubeRegistryConfiguration;
        private readonly IOcelotLogger logger;
        private readonly IKubeApiClient kubeApi;

        public Kube(KubeRegistryConfiguration kubeRegistryConfiguration, IOcelotLoggerFactory factory, IKubeApiClient kubeApi)
        {
            this.kubeRegistryConfiguration = kubeRegistryConfiguration;
            this.logger = factory.CreateLogger<Kube>();
            this.kubeApi = kubeApi;
        }

        public async Task<List<Service>> Get()
        {
            var endpoint = await kubeApi.EndPointsV1().Get(kubeRegistryConfiguration.KeyOfServiceInK8s, kubeRegistryConfiguration.KubeNamespace);
            var services = new List<Service>();
            if (endpoint != null && endpoint.Subsets.Any())
            {
                services.AddRange(BuildServices(endpoint));
            }
            else
            {
                logger.LogWarning($"namespace:{kubeRegistryConfiguration.KubeNamespace }service:{kubeRegistryConfiguration.KeyOfServiceInK8s} Unable to use ,it is invalid. Address must contain host only e.g. localhost and port must be greater than 0");
            }
            return services;
        }

        private List<Service> BuildServices(EndpointsV1 endpoint)
        {
            var services = new List<Service>();

            foreach (var subset in endpoint.Subsets)
            {
                services.AddRange(subset.Addresses.Select(address => new Service(endpoint.Metadata.Name,
                    new ServiceHostAndPort(address.Ip, subset.Ports.First().Port),
                    endpoint.Metadata.Uid, string.Empty, Enumerable.Empty<string>())));
            }
            return services;
        }
    }
}
