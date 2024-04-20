using KubeClient.Models;
using Ocelot.Logging;
using Ocelot.Provider.Kubernetes.Interfaces;
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
    private readonly IKubeServiceBuilder _serviceBuilder;
    private readonly List<Service> _services;

    public Kube(
        KubeRegistryConfiguration kubeRegistryConfiguration,
        IOcelotLoggerFactory factory,
        IKubeApiClient kubeApi,
        IKubeServiceBuilder serviceBuilder)
    {
        _kubeRegistryConfiguration = kubeRegistryConfiguration;
        _logger = factory.CreateLogger<Kube>();
        _kubeApi = kubeApi;
        _serviceBuilder = serviceBuilder;
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

    protected virtual IEnumerable<Service> BuildServices(EndpointsV1 endpoint)
        => _serviceBuilder.BuildServices(endpoint);
}
