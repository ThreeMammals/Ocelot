using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ocelot.Provider.Kubernetes.Interfaces;

namespace Ocelot.Provider.Kubernetes;

public class KubeApiClientFactory : IKubeApiClientFactory
{
    private readonly ILoggerFactory _logger;
    private readonly IOptions<KubeClientOptions> _options;

    public KubeApiClientFactory(ILoggerFactory logger, IOptions<KubeClientOptions> options)
    {
        _logger = logger;
        _options = options;
    }

    public string ServiceAccountPath { get; set; }

    public virtual KubeApiClient Get(bool usePodServiceAccount)
    {
        var options = usePodServiceAccount
            ? KubeClientOptions.FromPodServiceAccount(ServiceAccountPath)
            : _options.Value;
        options.LoggerFactory = _logger;
        return KubeApiClient.Create(options);
    }
}
