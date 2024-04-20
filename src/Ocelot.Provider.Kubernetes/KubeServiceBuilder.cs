using KubeClient.Models;
using Ocelot.Logging;
using Ocelot.Provider.Kubernetes.Interfaces;
using Ocelot.Values;

namespace Ocelot.Provider.Kubernetes;

public class KubeServiceBuilder : IKubeServiceBuilder
{
    private readonly IOcelotLogger _logger;

    public KubeServiceBuilder(IOcelotLoggerFactory factory)
    {
        _logger = factory.CreateLogger<KubeServiceBuilder>();
    }

    public virtual IEnumerable<Service> BuildServices(EndpointsV1 endpoint)
    {
        var services = new List<Service>();

        foreach (var subset in endpoint.Subsets)
        {
            services.AddRange(subset.Addresses.Select(address => new Service(
                endpoint.Metadata.Name,
                new ServiceHostAndPort(address.Ip, subset.Ports.First().Port),
                endpoint.Metadata.Uid,
                string.Empty,
                Enumerable.Empty<string>())));
        }

        _logger.LogDebug(() => $"K8s '{endpoint.Kind ?? "?"}:{endpoint.ApiVersion ?? "?"}:{endpoint.Metadata?.Name ?? endpoint.Metadata?.Namespace ?? "?"}' endpoint: Total built {services.Count} services.");
        return services;
    }
}
