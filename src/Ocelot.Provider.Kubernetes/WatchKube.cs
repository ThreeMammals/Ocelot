using KubeClient.Models;
using Ocelot.Logging;
using Ocelot.Provider.Kubernetes.Interfaces;
using Ocelot.Values;

namespace Ocelot.Provider.Kubernetes;

// Dispose() won't be called because provider wasn't resolved from DI
public class WatchKube : IServiceDiscoveryProvider, IDisposable
{
    private readonly KubeRegistryConfiguration _configuration;
    private readonly IOcelotLogger _logger;
    private readonly IKubeApiClient _kubeApi;
    private readonly IKubeServiceBuilder _serviceBuilder;
    
    private List<Service> _services = null;
    private readonly IDisposable _subscription;

    public WatchKube(
        KubeRegistryConfiguration configuration,
        IOcelotLoggerFactory factory,
        IKubeApiClient kubeApi,
        IKubeServiceBuilder serviceBuilder)
    {
        _configuration = configuration;
        _logger = factory.CreateLogger<Kube>();
        _kubeApi = kubeApi;
        _serviceBuilder = serviceBuilder;
        
        _subscription = CreateSubscription();
    }

    public virtual async Task<List<Service>> GetAsync()
    {
        // need to wait for first result fetching somehow
        if (_services is null)
        {
            await Task.Delay(1000);
        }

        if (_services is not { Count: > 0 })
        {
            _logger.LogWarning(() => GetMessage("Subscription to service endpoints gave no results!"));
        }

        return _services;
    }

    private IDisposable CreateSubscription() =>
        _kubeApi
            .EndpointsV1()
            .Watch(_configuration.KeyOfServiceInK8s, _configuration.KubeNamespace)
            .Subscribe(
                onNext: endpointEvent =>
                {
                    _services = endpointEvent.EventType switch
                    {
                        ResourceEventType.Deleted or ResourceEventType.Error => new(),
                        _ when (endpointEvent.Resource?.Subsets?.Count ?? 0) == 0 => new(),
                        _ => _serviceBuilder.BuildServices(_configuration, endpointEvent.Resource).ToList(),
                    };
                },
                onError: ex =>
                {
                    // recreate subscription in case of exceptions?
                    _logger.LogError(() => GetMessage("Endpoints subscription error occured"), ex);
                },
                onCompleted: () =>
                {
                    // called only when subscription is cancelled
                    _logger.LogWarning(() => GetMessage("Subscription to service endpoints completed"));
                });

    private string GetMessage(string message)
        => $"{nameof(WatchKube)} provider. Namespace:{_configuration.KubeNamespace}, Service:{_configuration.KeyOfServiceInK8s}; {message}";

    public void Dispose() => _subscription.Dispose();
}
