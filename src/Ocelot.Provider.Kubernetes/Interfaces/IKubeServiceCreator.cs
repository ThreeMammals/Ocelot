using KubeClient.Models;
using Ocelot.Values;

namespace Ocelot.Provider.Kubernetes.Interfaces;

public interface IKubeServiceCreator
{
    IEnumerable<Service> Create(EndpointsV1 endpoint, EndpointSubsetV1 subset);
}
