using KubeClient.Models;
using Ocelot.Infrastructure.DesignPatterns;
using Ocelot.Logging;
using Ocelot.Provider.Kubernetes.Interfaces;
using Ocelot.Values;

namespace Ocelot.Provider.Kubernetes;

/// <summary>Default Kubernetes service discovery provider.</summary>
/// <remarks>
/// <list type="bullet">
/// <item>NuGet: <see href="https://www.nuget.org/packages/KubeClient">KubeClient</see></item>
/// <item>GitHub: <see href="https://github.com/tintoy/dotnet-kube-client">dotnet-kube-client</see></item>
/// </list>
/// </remarks>
public class Kube : IServiceDiscoveryProvider
{
    private readonly KubeRegistryConfiguration _configuration;
    private readonly IOcelotLogger _logger;
    private readonly IKubeApiClient _kubeApi;
    private readonly IKubeServiceBuilder _serviceBuilder;

    public Kube(
        KubeRegistryConfiguration configuration,
        IOcelotLoggerFactory factory,
        IKubeApiClient kubeApi,
        IKubeServiceBuilder serviceBuilder)
    {
        _configuration = configuration;
        _logger = factory.CreateLogger<Kube>();
        _kubeApi = kubeApi;
        _serviceBuilder = serviceBuilder;
    }

    public virtual async Task<List<Service>> GetAsync()
    {
        var endpoint = await Retry.OperationAsync(GetEndpoint, Ensure, logger: _logger);

        if (Ensure(endpoint))
        {
            _logger.LogWarning(() => GetMessage($"Unable to use bad result returned by {nameof(Kube)} integration endpoint because the final result is invalid/unknown after multiple retries!"));
            return new(0);
        }

        return BuildServices(_configuration, endpoint)
            .ToList();
    }

    private Task<EndpointsV1> GetEndpoint() => _kubeApi
        .ResourceClient(client => new EndPointClientV1(client))
        .GetAsync(_configuration.KeyOfServiceInK8s, _configuration.KubeNamespace);

    private bool Ensure(EndpointsV1 endpoint)
    {
        if ((endpoint?.Subsets?.Count ?? 0) == 0)
        {
            _logger.LogWarning(() => GetMessage($"Endpoint ensuring has been failed! Endpoint object is null({endpoint == null}), or its subsets collection lenth is {endpoint?.Subsets?.Count ?? 0}."));
            return true;
        }

        return false;
    }

    private string GetMessage(string message) => $"{nameof(Kube)} provider. Namespace:{_configuration.KubeNamespace}, Service:{_configuration.KeyOfServiceInK8s}; {message}";

    protected virtual IEnumerable<Service> BuildServices(KubeRegistryConfiguration configuration, EndpointsV1 endpoint)
        => _serviceBuilder.BuildServices(configuration, endpoint);
}
