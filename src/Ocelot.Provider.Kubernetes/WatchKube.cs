﻿using KubeClient.Models;
using Ocelot.Logging;
using Ocelot.Provider.Kubernetes.Interfaces;
using Ocelot.Values;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Ocelot.Provider.Kubernetes;

public class WatchKube : IServiceDiscoveryProvider, IDisposable
{
    /// <summary>The default number of seconds to wait before scheduling the next retry for the subscription operation.</summary>
    /// <value>A positive integer that is greater than or equal to 1.</value>
    public static int FailedSubscriptionRetrySeconds
    {
        get => failedSubscriptionRetrySeconds;
        set => failedSubscriptionRetrySeconds = value >= 1 ? value : 1;
    }

    /// <summary>The default number of seconds to wait after Ocelot starts, following the provider's creation, to fetch the first result from the Kubernetes endpoint.</summary>
    /// <value>A positive integer that is greater than or equal to 1.</value>
    public static int FirstResultsFetchingTimeoutSeconds
    {
        get => firstResultsFetchingTimeoutSeconds;
        set => firstResultsFetchingTimeoutSeconds = value >= 1 ? value : 1;
    }

    private static int failedSubscriptionRetrySeconds = 1;
    private static int firstResultsFetchingTimeoutSeconds = 1;

    private readonly KubeRegistryConfiguration _configuration;
    private readonly IOcelotLogger _logger;
    private readonly IKubeApiClient _kubeApi;
    private readonly IKubeServiceBuilder _serviceBuilder;
    private readonly IScheduler _scheduler;
    
    private readonly IDisposable _subscription;
    private TaskCompletionSource _firstResultsCompletionSource;
    
    private List<Service> _services = new();

    public WatchKube(
        KubeRegistryConfiguration configuration,
        IOcelotLoggerFactory factory,
        IKubeApiClient kubeApi,
        IKubeServiceBuilder serviceBuilder,
        IScheduler scheduler)
    {
        _configuration = configuration;
        _logger = factory.CreateLogger<WatchKube>();
        _kubeApi = kubeApi;
        _serviceBuilder = serviceBuilder;
        _scheduler = scheduler;

        SetFirstResultsCompletedAfterDelay();
        _subscription = CreateSubscription();
    }

    public virtual async Task<List<Service>> GetAsync()
    {
        // Wait for first results fetching
        await _firstResultsCompletionSource.Task;
        if (_services.Count == 0)
        {
            _logger.LogWarning(() => GetMessage("Subscription to service endpoints gave no results!"));
        }

        return _services;
    }

    private void SetFirstResultsCompletedAfterDelay()
    {
        _firstResultsCompletionSource = new();
        Observable
            .Timer(TimeSpan.FromSeconds(FirstResultsFetchingTimeoutSeconds), _scheduler)
            .Subscribe(_ => _firstResultsCompletionSource.TrySetResult());
    }

    private void OnNext(IResourceEventV1<EndpointsV1> endpointEvent)
    {
        _services = endpointEvent.EventType switch
        {
            ResourceEventType.Deleted or ResourceEventType.Error => new(),
            _ when (endpointEvent.Resource?.Subsets.Count ?? 0) == 0 => new(),
            _ => _serviceBuilder.BuildServices(_configuration, endpointEvent.Resource).ToList(),
        };
        _firstResultsCompletionSource.TrySetResult();
    }

    // Called only when subscription canceled in Dispose
    private void OnCompleted() => _logger.LogInformation(() => GetMessage("Subscription to service endpoints completed"));
    private void OnException(Exception ex) => _logger.LogError(() => GetMessage("Endpoints subscription error occured."), ex);

    private IDisposable CreateSubscription() => _kubeApi
            .EndpointsV1()
            .Watch(_configuration.KeyOfServiceInK8s, _configuration.KubeNamespace)
            .Do(_ => { }, OnException)
            .RetryAfter(TimeSpan.FromSeconds(FailedSubscriptionRetrySeconds), _scheduler)
            .Subscribe(OnNext, OnCompleted);

    private string GetMessage(string message)
        => $"{nameof(WatchKube)} provider. Namespace:{_configuration.KubeNamespace}, Service:{_configuration.KeyOfServiceInK8s}; {message}";

    public void Dispose()
    {
        _subscription.Dispose();
        GC.SuppressFinalize(this);
    }
}
