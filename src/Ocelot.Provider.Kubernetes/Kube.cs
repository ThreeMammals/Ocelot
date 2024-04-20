using KubeClient.Models;
using Ocelot.Logging;
using Ocelot.Values;

namespace Ocelot.Provider.Kubernetes;

/// <summary>
/// Default Kubernetes service discovery provider.
/// </summary>
public class Kube : IServiceDiscoveryProvider
{
    private readonly KubeRegistryConfiguration _kubeRegistryConfiguration;
    private readonly IOcelotLogger _logger;
    private readonly IKubeApiClient _kubeApi;
    private readonly List<Service> _services;

    public Kube(KubeRegistryConfiguration kubeRegistryConfiguration, IOcelotLoggerFactory factory, IKubeApiClient kubeApi)
    {
        _kubeRegistryConfiguration = kubeRegistryConfiguration;
        _logger = factory.CreateLogger<Kube>();
        _kubeApi = kubeApi;
        _services = new();
    }

    public virtual async Task<List<Service>> GetAsync()
    {
        var endpoint = await _kubeApi
            .ResourceClient(client => new EndPointClientV1(client))
            .GetAsync(_kubeRegistryConfiguration.KeyOfServiceInK8s, _kubeRegistryConfiguration.KubeNamespace);

        _services.Clear();
        if (endpoint?.Subsets.Count != 0)
        {
            _services.AddRange(BuildServices(endpoint));
        }
        else
        {
            _logger.LogWarning(() => $"K8s Namespace:{_kubeRegistryConfiguration.KubeNamespace}, Service:{_kubeRegistryConfiguration.KeyOfServiceInK8s}; Unable to use: it is invalid. Address must contain host only e.g. localhost and port must be greater than 0!");
        }

        return _services;
    }

    protected virtual List<Service> BuildServices(EndpointsV1 endpoint)
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
