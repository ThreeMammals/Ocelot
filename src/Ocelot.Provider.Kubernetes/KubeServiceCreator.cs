using KubeClient.Models;
using Ocelot.Logging;
using Ocelot.Provider.Kubernetes.Interfaces;
using Ocelot.Values;

namespace Ocelot.Provider.Kubernetes;

public class KubeServiceCreator : IKubeServiceCreator
{
    private readonly IOcelotLogger _logger;

    public KubeServiceCreator(IOcelotLoggerFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _logger = factory.CreateLogger<KubeServiceCreator>();
    }

    public virtual IEnumerable<Service> Create(KubeRegistryConfiguration configuration, EndpointsV1 endpoint, EndpointSubsetV1 subset)
        => (configuration == null || endpoint == null || subset == null)
            ? Array.Empty<Service>()
            : subset.Addresses
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
        => endpoint.Metadata?.Name;

    protected virtual ServiceHostAndPort GetServiceHostAndPort(KubeRegistryConfiguration configuration, EndpointsV1 endpoint, EndpointSubsetV1 subset, EndpointAddressV1 address)
    {
        var ports = subset.Ports;
        bool portNameToScheme(EndpointPortV1 p) => string.Equals(p.Name, configuration.Scheme, StringComparison.InvariantCultureIgnoreCase);
        var portV1 = string.IsNullOrEmpty(configuration.Scheme) || !ports.Any(portNameToScheme)
            ? ports.FirstOrDefault()
            : ports.FirstOrDefault(portNameToScheme);
        portV1 ??= new();
        portV1.Name ??= configuration.Scheme ?? string.Empty;
        _logger.LogDebug(() => $"K8s service with key '{configuration.KeyOfServiceInK8s}' and address {address.Ip}; Detected port is {portV1.Name}:{portV1.Port}. Total {ports.Count} ports of [{string.Join(',', ports.Select(p => p.Name))}].");
        return new ServiceHostAndPort(address.Ip, portV1.Port, portV1.Name);
    }

    protected virtual string GetServiceId(KubeRegistryConfiguration configuration, EndpointsV1 endpoint, EndpointSubsetV1 subset, EndpointAddressV1 address)
        => endpoint.Metadata?.Uid;
    protected virtual string GetServiceVersion(KubeRegistryConfiguration configuration, EndpointsV1 endpoint, EndpointSubsetV1 subset, EndpointAddressV1 address)
        => endpoint.ApiVersion;
    protected virtual IEnumerable<string> GetServiceTags(KubeRegistryConfiguration configuration, EndpointsV1 endpoint, EndpointSubsetV1 subset, EndpointAddressV1 address)
        => Enumerable.Empty<string>();
}
