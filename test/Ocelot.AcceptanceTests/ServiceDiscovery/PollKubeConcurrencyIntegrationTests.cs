using KubeClient;
using KubeClient.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Ocelot.Logging;
using Ocelot.Provider.Kubernetes;
using Ocelot.Provider.Kubernetes.Interfaces;
using Ocelot.Values;
using System.Runtime.CompilerServices;

namespace Ocelot.AcceptanceTests.ServiceDiscovery;

/// <summary>
/// Concurrency integration tests for the PollKube service discovery provider.
/// <para>
/// Tests verify that the handler (Kubernetes API mock) is called the correct number of times based on polling intervals and concurrent request patterns.
/// </para><para>
/// The key metric is the handler counter which increments every time the Kubernetes API endpoint is called to fetch service endpoints.
/// </para>
/// </summary>
public class PollKubeConcurrencyIntegrationTests : Steps
{
    static JsonSerializerSettings JsonSerializerSettings => KubeClient.ResourceClients.KubeResourceClient.SerializerSettings;

    private readonly Mock<IOcelotLoggerFactory> _factory;
    private readonly Mock<IOcelotLogger> _logger;

    // Handler counter - tracks how many times the fake Kubernetes API was called
    private int _kubeHandlerCallCount;
#if NET9_0_OR_GREATER
    private static readonly Lock _counterLock = new();
#else
    private static readonly object _counterLock = new();
#endif

    private const int PollingInterval = 100; // milliseconds
    private const int FirstPollingWaitTime = 50; // milliseconds - less than polling interval
    private const int SecondPollingWaitTime = 150; // milliseconds - slightly more than polling interval

    public PollKubeConcurrencyIntegrationTests()
    {
        _factory = new();
        _logger = new();
        _factory.Setup(x => x.CreateLogger<PollKube>()).Returns(_logger.Object);
        _factory.Setup(x => x.CreateLogger<Kube>()).Returns(_logger.Object);
        _kubeHandlerCallCount = 0;
    }

    /// <summary>
    /// Scenario 1: Single call after start - Handler called once (counter = 1)
    /// 
    /// Arrange: Create PollKube provider
    /// Act: Make single GetAsync() call
    /// Assert: Handler counter should be 1 (cold start poll)
    /// </summary>
    [Fact(Skip = "Under development")]
    [Trait("Concurrency", "Scenario1")]
    [Trait("Feature", "PollingBehavior")]
    public async Task Scenario_1_SingleCallAfterStart_HandlerCalledOnce()
    {
        // Arrange
        _kubeHandlerCallCount = 0;
        var given = GivenClientAndPollKubeProvider(out var serviceBuilder);
        var expectedService = new Service("service-1", new("localhost", 8080), string.Empty, string.Empty, Array.Empty<string>());
        serviceBuilder.Setup(x => x.BuildServices(It.IsAny<KubeRegistryConfiguration>(), It.IsAny<EndpointsV1>()))
            .Returns(new[] { expectedService });

        var endpoints = GivenEndpointsWithVersion(1);
        GivenThereIsAFakeKubeServiceDiscoveryProvider(
            given.ClientOptions.ApiEndPoint.ToString(),
            given.ProviderOptions.KubeNamespace,
            given.ProviderOptions.KeyOfServiceInK8s,
            endpoints,
            out Lazy<string> _);

        // Act
        var services = await given.Provider.GetAsync();

        // Assert
        services.ShouldNotBeNull();
        services.Count.ShouldBe(1);
        _kubeHandlerCallCount.ShouldBe(1, "Handler should be called exactly once for cold start");
    }

    /// <summary>
    /// Scenario 2: Three parallel calls within polling interval (before 2nd polling)
    /// Handler called once (counter = 1)
    /// 
    /// Multiple parallel calls should return the same queued service version without triggering additional polls.
    /// All three calls occur before the next polling interval elapses.
    /// </summary>
    [Fact(Skip = "Under development")]
    [Trait("Concurrency", "Scenario2")]
    [Trait("Feature", "ParallelCalls")]
    public async Task Scenario_2_ThreeParallelCallsWithinFirstInterval_HandlerCalledOnce()
    {
        // Arrange
        _kubeHandlerCallCount = 0;
        var given = GivenClientAndPollKubeProvider(out var serviceBuilder);
        var version = 1;
        serviceBuilder.Setup(x => x.BuildServices(It.IsAny<KubeRegistryConfiguration>(), It.IsAny<EndpointsV1>()))
            .Returns(() => new Service[] { new($"service-v{version}", new("localhost", 8080), string.Empty, string.Empty, Array.Empty<string>()) });

        var endpoints = GivenEndpointsWithVersion(1);
        GivenThereIsAFakeKubeServiceDiscoveryProvider(
            given.ClientOptions.ApiEndPoint.ToString(),
            given.ProviderOptions.KubeNamespace,
            given.ProviderOptions.KeyOfServiceInK8s,
            endpoints,
            out Lazy<string> _);

        // Act
        // Make initial call to populate queue (cold start - this calls handler once)
        var firstCall = await given.Provider.GetAsync();
        firstCall.ShouldNotBeNull().ShouldNotBeEmpty();
        firstCall[0].Name.ShouldBe("service-v1");
        _kubeHandlerCallCount.ShouldBe(1, "First call should trigger cold start poll");

        // Make three parallel calls within the first polling interval (before 2nd polling)
        await Task.Delay(FirstPollingWaitTime, Xunit.TestContext.Current.CancellationToken); // Wait less than polling interval
        var parallelTasks = Enumerable.Range(0, 3)
            .Select(_ => given.Provider.GetAsync())
            .ToArray();
        var results = await Task.WhenAll(parallelTasks);

        // Assert
        // All three parallel calls should return the same version
        results.ShouldAllBe(r => r != null && r.Count == 1);
        results.ShouldAllBe(r => r[0].Name == "service-v1");

        // Handler should still be called only once - no additional polling occurred yet
        _kubeHandlerCallCount.ShouldBe(1, "Handler should not be called again - all parallel calls returned queued service");
    }

    /// <summary>
    /// Scenario 3: Multiple calls in 1st interval, 2nd polling returns new version
    /// Handler called twice (counter = 2)
    /// 
    /// First polling interval: multiple calls return version 1
    /// Second polling interval: handler is called again, returns version 2
    /// Then multiple calls return version 2
    /// </summary>
    [Fact(Skip = "Under development")]
    [Trait("Concurrency", "Scenario3")]
    [Trait("Feature", "PollingIntervals")]
    public async Task Scenario_3_FirstIntervalThenSecondPollingWithNewVersion_HandlerCalledTwice()
    {
        // Arrange
        _kubeHandlerCallCount = 0;
        var given = GivenClientAndPollKubeProvider(out var serviceBuilder);
        var version = 1;
        serviceBuilder.Setup(x => x.BuildServices(It.IsAny<KubeRegistryConfiguration>(), It.IsAny<EndpointsV1>()))
            .Returns(() => new Service[] { new($"service-v{version}", new("localhost", 8000 + version), string.Empty, string.Empty, Array.Empty<string>()) });

        var endpoints = GivenEndpointsWithVersion(1);
        GivenThereIsAFakeKubeServiceDiscoveryProvider(
            given.ClientOptions.ApiEndPoint.ToString(),
            given.ProviderOptions.KubeNamespace,
            given.ProviderOptions.KeyOfServiceInK8s,
            endpoints,
            out Lazy<string> _);

        // Act - First interval: make multiple calls
        var firstCall = await given.Provider.GetAsync();
        firstCall.ShouldNotBeNull();
        firstCall[0].Name.ShouldBe("service-v1");
        firstCall[0].HostAndPort.DownstreamPort.ShouldBe(8001);
        _kubeHandlerCallCount.ShouldBe(1);

        // Multiple calls within first interval
        await Task.Delay(FirstPollingWaitTime, Xunit.TestContext.Current.CancellationToken);
        var parallelCalls1 = await Task.WhenAll(
            Enumerable.Range(0, 3)
                .Select(_ => given.Provider.GetAsync())
                .ToArray());
        parallelCalls1.ShouldAllBe(r => r[0].Name == "service-v1");
        _kubeHandlerCallCount.ShouldBe(1, "No additional polling yet");

        // Act - Wait for 2nd polling interval to occur
        await Task.Delay(SecondPollingWaitTime, Xunit.TestContext.Current.CancellationToken); // Wait more than polling interval
        version = 2; // Simulate version update

        // Multiple calls in 2nd interval - should trigger second poll and return new version
        var secondIntervalCalls = await Task.WhenAll(
            Enumerable.Range(0, 3)
                .Select(_ => given.Provider.GetAsync())
                .ToArray());

        // Assert
        secondIntervalCalls.ShouldAllBe(r => r[0].Name == "service-v2");
        secondIntervalCalls.ShouldAllBe(r => r[0].HostAndPort.DownstreamPort == 8002);

        // Handler should have been called during the 2nd polling interval
        _kubeHandlerCallCount.ShouldBe(2, "2nd polling should have called handler");
    }

    /// <summary>
    /// Scenario 4: Multiple calls in each interval, polling returns new version each time
    /// Handler called during each polling, counter increased by 1 for each poll
    /// 
    /// This tests that polling happens at regular intervals and each poll increments the counter.
    /// </summary>
    [Fact(Skip = "Under development")]
    [Trait("Concurrency", "Scenario4")]
    [Trait("Feature", "RegularPolling")]
    public async Task Scenario_4_MultipleCallsEachIntervalNewVersionEachPolling_CounterIncreasedByOne()
    {
        // Arrange
        _kubeHandlerCallCount = 0;
        var given = GivenClientAndPollKubeProvider(out var serviceBuilder);
        var version = 1;
        serviceBuilder.Setup(x => x.BuildServices(It.IsAny<KubeRegistryConfiguration>(), It.IsAny<EndpointsV1>()))
            .Returns(() => new Service[] { new($"service-v{version}", new("localhost", 9000 + version), string.Empty, string.Empty, Array.Empty<string>()) });

        var endpoints = GivenEndpointsWithVersion(1);
        GivenThereIsAFakeKubeServiceDiscoveryProvider(
            given.ClientOptions.ApiEndPoint.ToString(),
            given.ProviderOptions.KubeNamespace,
            given.ProviderOptions.KeyOfServiceInK8s,
            endpoints,
            out Lazy<string> _);

        // Act & Assert - Interval 1
        var call1 = await given.Provider.GetAsync();
        call1[0].Name.ShouldBe("service-v1");
        _kubeHandlerCallCount.ShouldBe(1);

        // Parallel calls in interval 1
        await Task.Delay(FirstPollingWaitTime, Xunit.TestContext.Current.CancellationToken);
        var interval1Calls = await Task.WhenAll(
            Enumerable.Range(0, 2).Select(_ => given.Provider.GetAsync()).ToArray());
        interval1Calls.ShouldAllBe(r => r[0].Name == "service-v1");
        _kubeHandlerCallCount.ShouldBe(1);

        // Act & Assert - Interval 2
        await Task.Delay(SecondPollingWaitTime, Xunit.TestContext.Current.CancellationToken);
        version = 2;
        var interval2Calls = await Task.WhenAll(
            Enumerable.Range(0, 2).Select(_ => given.Provider.GetAsync()).ToArray());
        interval2Calls.ShouldAllBe(r => r[0].Name == "service-v2");
        _kubeHandlerCallCount.ShouldBe(2, "Counter should be 2 after 2nd polling");

        // Act & Assert - Interval 3
        await Task.Delay(SecondPollingWaitTime, Xunit.TestContext.Current.CancellationToken);
        version = 3;
        var interval3Calls = await Task.WhenAll(
            Enumerable.Range(0, 2).Select(_ => given.Provider.GetAsync()).ToArray());
        interval3Calls.ShouldAllBe(r => r[0].Name == "service-v3");
        _kubeHandlerCallCount.ShouldBe(3, "Counter should be 3 after 3rd polling");

        // Act & Assert - Interval 4
        await Task.Delay(SecondPollingWaitTime, Xunit.TestContext.Current.CancellationToken);
        version = 4;
        var interval4Calls = await Task.WhenAll(
            Enumerable.Range(0, 2).Select(_ => given.Provider.GetAsync()).ToArray());
        interval4Calls.ShouldAllBe(r => r[0].Name == "service-v4");
        _kubeHandlerCallCount.ShouldBe(4, "Counter should be 4 after 4th polling");
    }

    /// <summary>
    /// Scenario 5: Heavy load (~1000 concurrent calls) during each polling interval
    /// Handler called twice (counter = 2)
    /// 
    /// Heavy load doesn't impact polling which happens at regular intervals.
    /// Even with 1000 concurrent calls, the handler should be called exactly twice
    /// (once for cold start, once for 2nd polling interval).
    /// </summary>
    [Fact(Skip = "Under development")]
    [Trait("Concurrency", "Scenario5")]
    [Trait("Feature", "HeavyLoad")]
    [Trait("Performance", "Stress")]
    public async Task Scenario_5_HeavyLoad1000CallsEachInterval_HandlerCalledTwice()
    {
        // Arrange
        _kubeHandlerCallCount = 0;
        var given = GivenClientAndPollKubeProvider(out var serviceBuilder);
        var version = 1;
        var callCount = 0;
        serviceBuilder.Setup(x => x.BuildServices(It.IsAny<KubeRegistryConfiguration>(), It.IsAny<EndpointsV1>()))
            .Returns(() =>
            {
                Interlocked.Increment(ref callCount);
                return new Service[] { new($"service-v{version}", new("localhost", 7000 + version), string.Empty, string.Empty, Array.Empty<string>()) };
            });

        var endpoints = GivenEndpointsWithVersion(1);
        GivenThereIsAFakeKubeServiceDiscoveryProvider(
            given.ClientOptions.ApiEndPoint.ToString(),
            given.ProviderOptions.KubeNamespace,
            given.ProviderOptions.KeyOfServiceInK8s,
            endpoints,
            out Lazy<string> _);

        // Act - Cold start with heavy load
        var coldStartHeavyLoad = await Task.WhenAll(
            Enumerable.Range(0, 1000)
                .Select(_ => given.Provider.GetAsync())
                .ToArray());

        // Assert - Cold start
        coldStartHeavyLoad.ShouldAllBe(r => r != null && r.Count == 1);
        // With heavy load, multiple threads might poll, but ideally only once
        var coldStartCount = _kubeHandlerCallCount;
        coldStartCount.ShouldBe(1, "Cold start should call handler once despite 1000 concurrent calls");

        // Act - Wait for 2nd polling interval with heavy load
        await Task.Delay(SecondPollingWaitTime, Xunit.TestContext.Current.CancellationToken);
        version = 2;

        var secondIntervalHeavyLoad = await Task.WhenAll(
            Enumerable.Range(0, 1000)
                .Select(_ => given.Provider.GetAsync())
                .ToArray());

        // Assert - 2nd polling
        secondIntervalHeavyLoad.ShouldAllBe(r => r != null && r.Count == 1);
        secondIntervalHeavyLoad.ShouldAllBe(r => r[0].Name == "service-v2");

        // Handler should be called exactly twice despite the heavy load
        _kubeHandlerCallCount.ShouldBe(2,
            "Handler should be called exactly twice (cold start + 1st polling) despite 1000 concurrent calls per interval");
    }

    /// <summary>
    /// Extended Scenario 5b: Verify all responses are consistent during heavy load
    /// 
    /// Even under heavy load with 1000+ concurrent calls, all responses should be identical
    /// until the polling interval updates the services.
    /// </summary>
    [Fact(Skip = "Under development")]
    [Trait("Concurrency", "Scenario5Extended")]
    [Trait("Feature", "ConsistencyUnderLoad")]
    public async Task Scenario_5b_HeavyLoadAllResponsesConsistent_NoPartialUpdates()
    {
        // Arrange
        _kubeHandlerCallCount = 0;
        var given = GivenClientAndPollKubeProvider(out var serviceBuilder);
        var version = 1;
        serviceBuilder.Setup(x => x.BuildServices(It.IsAny<KubeRegistryConfiguration>(), It.IsAny<EndpointsV1>()))
            .Returns(() => new Service[] { new($"service-v{version}", new("localhost", 6000 + version), string.Empty, string.Empty, Array.Empty<string>()) });

        var endpoints = GivenEndpointsWithVersion(1);
        GivenThereIsAFakeKubeServiceDiscoveryProvider(
            given.ClientOptions.ApiEndPoint.ToString(),
            given.ProviderOptions.KubeNamespace,
            given.ProviderOptions.KeyOfServiceInK8s,
            endpoints,
            out Lazy<string> _);

        // Act - Cold start
        var coldStartResponses = await Task.WhenAll(
            Enumerable.Range(0, 500)
                .Select(_ => given.Provider.GetAsync())
                .ToArray());

        // Assert - All responses should be for version 1
        var v1Count = coldStartResponses.Count(r => r[0].Name == "service-v1");
        v1Count.ShouldBe(500, "All cold start responses should be version 1");
        _kubeHandlerCallCount.ShouldBe(1);

        // Act - Wait and trigger 2nd polling
        await Task.Delay(SecondPollingWaitTime, Xunit.TestContext.Current.CancellationToken);
        version = 2;

        // Heavy load that might span polling interval boundary
        var heavyLoadResponses = await Task.WhenAll(
            Enumerable.Range(0, 1500)
                .Select(async (i) =>
                {
                    if (i % 500 == 0)
                        await Task.Delay(10); // Small delay to allow polling to happen
                    return await given.Provider.GetAsync();
                })
                .ToArray());

        // Assert - All responses should be version 1 (queued before 2nd polling)
        // or version 2 (after 2nd polling), not a mix
        var v2Count = heavyLoadResponses.Count(r => r[0].Name == "service-v2");
        var allV1 = heavyLoadResponses.All(r => r[0].Name == "service-v1");
        var allV2 = heavyLoadResponses.All(r => r[0].Name == "service-v2");

        (allV1 || allV2).ShouldBeTrue("All responses should be consistent - either all v1 or all v2");

        // Handler should be called twice total
        _kubeHandlerCallCount.ShouldBe(2, "Should have exactly 2 handler calls");
    }

    #region Helper Methods

    private (IKubeApiClient Client, KubeClientOptions ClientOptions, PollKube Provider, KubeRegistryConfiguration ProviderOptions)
        GivenClientAndPollKubeProvider(out Mock<IKubeServiceBuilder> serviceBuilder, int pollingInterval = PollingInterval, [CallerMemberName] string serviceName = null)
    {
        serviceName ??= serviceName;
        var kubePort = PortFinder.GetRandomPort();
        var options = new KubeClientOptions
        {
            AccessToken = serviceName, // "txpc696iUhbVoudg164r93CxDTrKRVWG",
            AllowInsecure = true,
            ApiEndPoint = new(DownstreamUrl(kubePort)),
            AuthStrategy = KubeAuthStrategy.BearerToken,
        };

        IKubeApiClient client = KubeApiClient.Create(options);

        var config = new KubeRegistryConfiguration
        {
            KeyOfServiceInK8s = serviceName,
            KubeNamespace = nameof(PollKubeConcurrencyIntegrationTests),
        };

        serviceBuilder = new();
        var kubeProvider = new Kube(config, _factory.Object, client, serviceBuilder.Object);
        var provider = new PollKube(pollingInterval, _factory.Object, kubeProvider);

        return (client, options, provider, config);
    }

    protected void GivenThereIsAFakeKubeServiceDiscoveryProvider(
        string url, string namespaces, string serviceName, EndpointsV1 endpointEntries, out Lazy<string> receivedToken)
    {
        var token = string.Empty;
        receivedToken = new(() => token);
        handler.GivenThereIsAServiceRunningOn(url, ProcessKubernetesRequest);

        Task ProcessKubernetesRequest(HttpContext context)
        {
            if (context.Request.Path.Value == $"/api/v1/namespaces/{namespaces}/endpoints/{serviceName}")
            {
                // Increment handler call counter - this is the key metric being tested
                lock (_counterLock)
                {
                    _kubeHandlerCallCount++;
                }

                if (context.Request.Headers.TryGetValue("Authorization", out var values))
                {
                    token = values.First();
                }

                var responseBody = JsonConvert.SerializeObject(endpointEntries, JsonSerializerSettings);
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.Headers.Append("Content-Type", "application/json");
                return context.Response.WriteAsync(responseBody);
            }

            return Task.CompletedTask;
        }
    }

    private static EndpointsV1 GivenEndpointsWithVersion(int version)
    {
        var endpoints = new EndpointsV1
        {
            Metadata = new ObjectMetaV1
            {
                Name = "test-endpoints",
                Namespace = nameof(PollKubeConcurrencyIntegrationTests),
                Generation = version,
            },
        };
        var subset = new EndpointSubsetV1();
        endpoints.Subsets.Add(subset);
        subset.Addresses.Add(new()
        {
            Ip = "127.0.0.1",
            TargetRef = new ObjectReferenceV1 { Name = "pod-1" },
        });
        subset.Ports.Add(new()
        {
            Name = "http",
            Port = 8080,
        });
        return endpoints;
    }
    #endregion
}
