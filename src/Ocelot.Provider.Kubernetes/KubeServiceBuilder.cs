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
        ArgumentNullException.ThrowIfNull(factory);
        _logger = factory.CreateLogger<KubeServiceBuilder>();

        ArgumentNullException.ThrowIfNull(serviceCreator);
        _serviceCreator = serviceCreator;
    }

    public virtual IEnumerable<Service> BuildServices(KubeRegistryConfiguration configuration, EndpointsV1 endpoint)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(endpoint);

        var services = endpoint.Subsets
            .SelectMany(subset => _serviceCreator.Create(configuration, endpoint, subset))
            .ToArray();

        _logger.LogDebug(() => $"K8s '{Check(endpoint.Kind)}:{Check(endpoint.ApiVersion)}:{Check(endpoint.Metadata?.Name)}' endpoint: Total built {services.Length} services.");
        return services;
    }

    private static string Check(string str) => string.IsNullOrEmpty(str) ? "?" : str;
}
