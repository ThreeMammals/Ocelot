using KubeClient.Models;
using Ocelot.Logging;
using Ocelot.Provider.Kubernetes.Interfaces;
using Ocelot.Values;

namespace Ocelot.Provider.Kubernetes;

public class KubeServiceBuilder : IKubeServiceBuilder
{
    private readonly IOcelotLogger _logger;
    private readonly IKubeServiceCreator _serviceCreator;


    public KubeServiceBuilder(IOcelotLoggerFactory factory, IKubeServiceCreator serviceCreator)
    {
        _logger = factory.CreateLogger<KubeServiceBuilder>();
        _serviceCreator = serviceCreator;
    }

    public virtual IEnumerable<Service> BuildServices(KubeRegistryConfiguration configuration, EndpointsV1 endpoint)
    {
        var services = endpoint.Subsets
            .SelectMany(subset => _serviceCreator.Create(configuration, endpoint, subset))
            .ToArray();
        _logger.LogDebug(() => $"K8s '{endpoint.Kind ?? "?"}:{endpoint.ApiVersion ?? "?"}:{endpoint.Metadata?.Name ?? endpoint.Metadata?.Namespace ?? "?"}' endpoint: Total built {services.Length} services.");
        return services;
    }
}
