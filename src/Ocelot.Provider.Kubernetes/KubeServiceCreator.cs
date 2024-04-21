using KubeClient.Models;
using Ocelot.Provider.Kubernetes.Interfaces;
using Ocelot.Values;

namespace Ocelot.Provider.Kubernetes;

public class KubeServiceCreator : IKubeServiceCreator
{
    public virtual IEnumerable<Service> Create(KubeRegistryConfiguration configuration, EndpointsV1 endpoint, EndpointSubsetV1 subset)
        => subset.Addresses
            .SelectMany(address => CreateInstance(configuration, endpoint, subset, address))
            .ToArray();

    public virtual IEnumerable<Service> CreateInstance(KubeRegistryConfiguration configuration, EndpointsV1 endpoint, EndpointSubsetV1 subset, EndpointAddressV1 address)
    {
        var instance = new Service(
            GetServiceName(configuration, endpoint, subset, address),
            GetServiceHostAndPort(configuration, endpoint, subset, address),
            GetServiceId(configuration, endpoint, subset, address),
            GetServiceVersion(configuration, endpoint, subset, address),
            GetServiceTags(configuration, endpoint, subset, address)
        );
        return new Service[] { instance };
    }

    protected virtual string GetServiceName(KubeRegistryConfiguration configuration, EndpointsV1 endpoint, EndpointSubsetV1 subset, EndpointAddressV1 address)
        => endpoint.Metadata.Name;

    protected virtual ServiceHostAndPort GetServiceHostAndPort(KubeRegistryConfiguration configuration, EndpointsV1 endpoint, EndpointSubsetV1 subset, EndpointAddressV1 address)
    {
        var ports = subset.Ports;
        bool portNameToScheme(EndpointPortV1 p) => p.Name.Equals(configuration.Scheme, StringComparison.InvariantCultureIgnoreCase);
        var portV1 = string.IsNullOrEmpty(configuration.Scheme) || !ports.Any(portNameToScheme)
            ? ports.First()
            : ports.First(portNameToScheme);
        return new ServiceHostAndPort(address.Ip, portV1.Port, portV1.Name);
    }

    protected virtual string GetServiceId(KubeRegistryConfiguration configuration, EndpointsV1 endpoint, EndpointSubsetV1 subset, EndpointAddressV1 address)
        => endpoint.Metadata.Uid;
    protected virtual string GetServiceVersion(KubeRegistryConfiguration configuration, EndpointsV1 endpoint, EndpointSubsetV1 subset, EndpointAddressV1 address)
        => endpoint.ApiVersion;
    protected virtual IEnumerable<string> GetServiceTags(KubeRegistryConfiguration configuration, EndpointsV1 endpoint, EndpointSubsetV1 subset, EndpointAddressV1 address)
        => Enumerable.Empty<string>();
}
