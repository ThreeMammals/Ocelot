using KubeClient.Models;
using Ocelot.Provider.Kubernetes.Interfaces;
using Ocelot.Values;

namespace Ocelot.Provider.Kubernetes;

public class KubeServiceCreator : IKubeServiceCreator
{
    public virtual IEnumerable<Service> Create(EndpointsV1 endpoint, EndpointSubsetV1 subset)
        => subset.Addresses
            .Select(address => CreateInstance(endpoint, subset, address))
            .ToArray();

    public virtual Service CreateInstance(EndpointsV1 endpoint, EndpointSubsetV1 subset, EndpointAddressV1 address)
        => new(
            endpoint.Metadata.Name,
            new ServiceHostAndPort(address.Ip, subset.Ports.First().Port),
            endpoint.Metadata.Uid,
            endpoint.ApiVersion,
            Enumerable.Empty<string>()
        );
}
