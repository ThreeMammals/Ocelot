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

    public Kube(KubeRegistryConfiguration kubeRegistryConfiguration, IOcelotLoggerFactory factory, IKubeApiClient kubeApi)
    {
        _kubeRegistryConfiguration = kubeRegistryConfiguration;
        _logger = factory.CreateLogger<Kube>();
        _kubeApi = kubeApi;
    }

    public async Task<List<Service>> GetAsync()
    {
        var endpoint = await _kubeApi
            .ResourceClient(client => new EndPointClientV1(client))
            .GetAsync(_kubeRegistryConfiguration.KeyOfServiceInK8s, _kubeRegistryConfiguration.KubeNamespace);

        var services = new List<Service>();
        if (endpoint != null && endpoint.Subsets.Any())
        {
            services.AddRange(BuildServices(endpoint));
        }
        else
        {
            _logger.LogWarning(() => $"namespace:{_kubeRegistryConfiguration.KubeNamespace}service:{_kubeRegistryConfiguration.KeyOfServiceInK8s} Unable to use ,it is invalid. Address must contain host only e.g. localhost and port must be greater than 0");
        }

        return services;
    }

    private static List<Service> BuildServices(EndpointsV1 endpoint)
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
