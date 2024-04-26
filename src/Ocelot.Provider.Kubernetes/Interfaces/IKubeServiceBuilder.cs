using KubeClient.Models;
using Ocelot.Values;

namespace Ocelot.Provider.Kubernetes.Interfaces;

public interface IKubeServiceBuilder
{
    IEnumerable<Service> BuildServices(KubeRegistryConfiguration configuration, EndpointsV1 endpoint);
}
