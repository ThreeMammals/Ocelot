using KubeClient.Models;
using KubeClient.ResourceClients;

namespace Ocelot.Provider.Kubernetes.Interfaces;

public interface IEndPointClient : IKubeResourceClient
{
    Task<EndpointsV1> GetAsync(string serviceName, string kubeNamespace = null, CancellationToken cancellationToken = default);
}
