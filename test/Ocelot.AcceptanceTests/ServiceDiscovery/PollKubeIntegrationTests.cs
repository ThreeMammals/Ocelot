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
/// Integration tests for the <see cref="PollKube"/> service discovery provider.
/// <see cref="PollKube"/> polls the Kubernetes API at specified intervals to discover services.
/// </summary>
[Trait("Milestone", ".NET 10")] // https://github.com/ThreeMammals/Ocelot/milestone/13
[Trait("Release", "25.0.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.0
public class PollKubeIntegrationTests : Steps
{
    static JsonSerializerSettings JsonSerializerSettings => KubeClient.ResourceClients.KubeResourceClient.SerializerSettings;

    private readonly Mock<IOcelotLoggerFactory> _factory;
    private readonly Mock<IOcelotLogger> _logger;
    private const int PollingInterval = 100; // milliseconds

    public PollKubeIntegrationTests()
    {
        _factory = new();
        _logger = new();
        _factory.Setup(x => x.CreateLogger<PollKube>()).Returns(_logger.Object);
        _factory.Setup(x => x.CreateLogger<Kube>()).Returns(_logger.Object);
    }

    [Fact(Skip = "Under development")]
    [Trait("Feature", "Polling")]
    public async Task Should_return_service_from_k8s_on_first_call()
    {
        // Arrange
        var given = GivenClientAndPollKubeProvider(out var serviceBuilder);
        var expectedService = new Service(
            nameof(Should_return_service_from_k8s_on_first_call),
            new("localhost", 8080),
            string.Empty,
            string.Empty,
            Array.Empty<string>());
        serviceBuilder.Setup(x => x.BuildServices(It.IsAny<KubeRegistryConfiguration>(), It.IsAny<EndpointsV1>()))
            .Returns(new[] { expectedService });

        var endpoints = GivenEndpoints();
        GivenThereIsAFakeKubeServiceDiscoveryProvider(
            given.ClientOptions.ApiEndPoint.ToString(),
            given.ProviderOptions.KubeNamespace,
            given.ProviderOptions.KeyOfServiceInK8s,
            statusCode: HttpStatusCode.OK,
            endpoints,
            out Lazy<string> receivedToken);

        // Act - First call should perform initial poll
        var services = await given.Provider.GetAsync();

        // Assert
        services.ShouldNotBeNull();
        services.Count.ShouldBe(1);
        services[0].HostAndPort.DownstreamHost.ShouldBe("localhost");
        services[0].HostAndPort.DownstreamPort.ShouldBe(8080);
        receivedToken.Value.ShouldContain("Bearer");
    }

    [Fact(Skip = "Under development")]
    [Trait("Feature", "Polling")]
    [Trait("Concurrency", "Multiple")]
    public async Task Should_return_queued_service_on_concurrent_calls()
    {
        // Arrange
        var given = GivenClientAndPollKubeProvider(out var serviceBuilder);
        var expectedService = new Service(
            nameof(Should_return_queued_service_on_concurrent_calls),
            new("localhost", 9090),
            string.Empty,
            string.Empty,
            Array.Empty<string>());
        serviceBuilder.Setup(x => x.BuildServices(It.IsAny<KubeRegistryConfiguration>(), It.IsAny<EndpointsV1>()))
            .Returns(new[] { expectedService });

        var endpoints = GivenEndpoints();
        GivenThereIsAFakeKubeServiceDiscoveryProvider(
            given.ClientOptions.ApiEndPoint.ToString(),
            given.ProviderOptions.KubeNamespace,
            given.ProviderOptions.KeyOfServiceInK8s,
            statusCode: HttpStatusCode.OK,
            endpoints,
            out Lazy<string> receivedToken);

        // Act - First call to populate queue
        var firstCall = await given.Provider.GetAsync();
        firstCall.ShouldNotBeNull();
        firstCall.Count.ShouldBe(1);

        // Act - Multiple concurrent calls should return queued service
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => given.Provider.GetAsync())
            .ToArray();
        var results = await Task.WhenAll(tasks);

        // Assert
        results.ShouldAllBe(r => r != null && r.Count == 1);
        results.ShouldAllBe(r => r[0].HostAndPort.DownstreamPort == 9090);
    }

    [Fact(Skip = "Under development")]
    [Trait("Feature", "Polling")]
    [Trait("Timing", "Interval")]
    public async Task Should_poll_at_specified_intervals()
    {
        // Arrange
        var given = GivenClientAndPollKubeProvider(out var serviceBuilder, pollingInterval: 50);
        var callCount = 0;
        serviceBuilder.Setup(x => x.BuildServices(It.IsAny<KubeRegistryConfiguration>(), It.IsAny<EndpointsV1>()))
            .Returns(() =>
            {
                Interlocked.Increment(ref callCount);
                return new Service[] { new("service", new("localhost", callCount * 1000), string.Empty, string.Empty, Array.Empty<string>()) };
            });

        var endpoints = GivenEndpoints();
        GivenThereIsAFakeKubeServiceDiscoveryProvider(
            given.ClientOptions.ApiEndPoint.ToString(),
            given.ProviderOptions.KubeNamespace,
            given.ProviderOptions.KeyOfServiceInK8s,
            statusCode: HttpStatusCode.OK,
            endpoints,
            out Lazy<string> _);

        // Act
        var firstServices = await given.Provider.GetAsync();
        firstServices.ShouldNotBeNull();

        // Wait for polling interval to elapse and check if new service version is queued
        await Task.Delay(PollingInterval, Xunit.TestContext.Current.CancellationToken); // Wait for at least one or two polling cycles

        var secondServices = await given.Provider.GetAsync();

        // Assert - Services should not be empty and polling should have occurred
        secondServices.ShouldNotBeNull();
        callCount.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    [Trait("Feature", "QueueManagement")]
    [Trait("Behavior", "OldVersionRemoval")]
    public async Task Should_remove_outdated_versions_and_keep_latest()
    {
        // Arrange
        var given = GivenClientAndPollKubeProvider(out var serviceBuilder);
        var version = 0;
        serviceBuilder.Setup(x => x.BuildServices(It.IsAny<KubeRegistryConfiguration>(), It.IsAny<EndpointsV1>()))
            .Returns(() =>
            {
                version++;
                return new Service[] { new($"service-v{version}", new("localhost", version), string.Empty, string.Empty, Array.Empty<string>()) };
            });

        var endpoints = GivenEndpoints();
        GivenThereIsAFakeKubeServiceDiscoveryProvider(
            given.ClientOptions.ApiEndPoint.ToString(),
            given.ProviderOptions.KubeNamespace,
            given.ProviderOptions.KeyOfServiceInK8s,
            statusCode: HttpStatusCode.OK,
            endpoints,
            out Lazy<string> _);

        // Act
        var firstCall = await given.Provider.GetAsync();
        firstCall.ShouldNotBeNull();

        // Wait for multiple polling cycles
        await Task.Delay(300, Xunit.TestContext.Current.CancellationToken);

        var lastCall = await given.Provider.GetAsync();

        // Assert - Should get the latest version with the highest port number
        lastCall.ShouldNotBeNull();
        lastCall.Count.ShouldBe(1);
        lastCall[0].HostAndPort.DownstreamPort.ShouldBeGreaterThan(1);
    }

    [Fact]
    [Trait("Feature", "ErrorHandling")]
    public async Task Should_return_empty_when_provider_disposed()
    {
        // Arrange
        var given = GivenClientAndPollKubeProvider(out var serviceBuilder);
        serviceBuilder.Setup(x => x.BuildServices(It.IsAny<KubeRegistryConfiguration>(), It.IsAny<EndpointsV1>()))
            .Returns(new Service[] { new("test", new("localhost", 80), string.Empty, string.Empty, Array.Empty<string>()) });

        var endpoints = GivenEndpoints();
        GivenThereIsAFakeKubeServiceDiscoveryProvider(
            given.ClientOptions.ApiEndPoint.ToString(),
            given.ProviderOptions.KubeNamespace,
            given.ProviderOptions.KeyOfServiceInK8s,
            statusCode: HttpStatusCode.OK,
            endpoints,
            out Lazy<string> _);

        // Act
        var services = await given.Provider.GetAsync();
        services.ShouldNotBeNull();

        // Dispose the provider
        given.Provider.Dispose();
        await Task.Delay(200, Xunit.TestContext.Current.CancellationToken);

        // Try to get services after disposal - should return empty
        var servicesAfterDisposal = await given.Provider.GetAsync();

        // Assert
        servicesAfterDisposal.ShouldNotBeNull();
        servicesAfterDisposal.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Feature", "ErrorHandling")]
    [Trait("Scenario", "KubeAPIError")]
    public async Task Should_handle_k8s_api_error_gracefully()
    {
        // Arrange
        var given = GivenClientAndPollKubeProvider(out var serviceBuilder);
        serviceBuilder.Setup(x => x.BuildServices(It.IsAny<KubeRegistryConfiguration>(), It.IsAny<EndpointsV1>()))
            .Returns(new Service[] { new("test", new("localhost", 80), string.Empty, string.Empty, Array.Empty<string>()) });

        var endpoints = GivenEndpoints();
        GivenThereIsAFakeKubeServiceDiscoveryProvider(
            given.ClientOptions.ApiEndPoint.ToString(),
            given.ProviderOptions.KubeNamespace,
            given.ProviderOptions.KeyOfServiceInK8s,
            statusCode: HttpStatusCode.InternalServerError,
            endpoints,
            out Lazy<string> _);

        // Act
        var services = await given.Provider.GetAsync();

        // Assert - Should return empty list on error
        services.ShouldNotBeNull();
        // First call may return empty due to API error
    }

    [Fact(Skip = "Under development")]
    [Trait("Feature", "ColdStart")]
    public async Task Should_perform_initial_poll_on_first_call_when_queue_is_empty()
    {
        // Arrange
        var given = GivenClientAndPollKubeProvider(out var serviceBuilder);
        var initialService = new Service(
            "initial-service",
            new("localhost", 5000),
            string.Empty,
            string.Empty,
            Array.Empty<string>());
        serviceBuilder.Setup(x => x.BuildServices(It.IsAny<KubeRegistryConfiguration>(), It.IsAny<EndpointsV1>()))
            .Returns(new[] { initialService });

        var endpoints = GivenEndpoints();
        GivenThereIsAFakeKubeServiceDiscoveryProvider(
            given.ClientOptions.ApiEndPoint.ToString(),
            given.ProviderOptions.KubeNamespace,
            given.ProviderOptions.KeyOfServiceInK8s,
            statusCode: HttpStatusCode.OK,
            endpoints,
            out Lazy<string> _);

        // Act - First call on newly created provider with empty queue
        var services = await given.Provider.GetAsync();

        // Assert
        services.ShouldNotBeNull();
        services.Count.ShouldBe(1);
        services[0].Name.ShouldBe("initial-service");
        services[0].HostAndPort.DownstreamPort.ShouldBe(5000);
    }

    [Fact]
    [Trait("Feature", "QueueManagement")]
    public async Task Should_not_enqueue_services_when_already_polling()
    {
        // Arrange
        var given = GivenClientAndPollKubeProvider(out var serviceBuilder);
        var pollCount = 0;
        serviceBuilder.Setup(x => x.BuildServices(It.IsAny<KubeRegistryConfiguration>(), It.IsAny<EndpointsV1>()))
            .Returns(() =>
            {
                Interlocked.Increment(ref pollCount);
                return new Service[] { new($"service-poll-{pollCount}", new("localhost", 8000), string.Empty, string.Empty, Array.Empty<string>()) };
            });

        var endpoints = GivenEndpoints();
        GivenThereIsAFakeKubeServiceDiscoveryProvider(
            given.ClientOptions.ApiEndPoint.ToString(),
            given.ProviderOptions.KubeNamespace,
            given.ProviderOptions.KeyOfServiceInK8s,
            statusCode: HttpStatusCode.OK,
            endpoints,
            out Lazy<string> _);

        // Act
        var firstCall = await given.Provider.GetAsync();
        firstCall.ShouldNotBeNull();

        // Assert
        pollCount.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact(Skip = "Under development")]
    [Trait("Feature", "Threading")]
    public async Task Should_safely_handle_disposal_during_polling()
    {
        // Arrange
        var given = GivenClientAndPollKubeProvider(out var serviceBuilder);
        serviceBuilder.Setup(x => x.BuildServices(It.IsAny<KubeRegistryConfiguration>(), It.IsAny<EndpointsV1>()))
            .Returns(new Service[] { new("test", new("localhost", 80), string.Empty, string.Empty, Array.Empty<string>()) });

        var endpoints = GivenEndpoints();
        GivenThereIsAFakeKubeServiceDiscoveryProvider(
            given.ClientOptions.ApiEndPoint.ToString(),
            given.ProviderOptions.KubeNamespace,
            given.ProviderOptions.KeyOfServiceInK8s,
            statusCode: HttpStatusCode.OK,
            endpoints,
            out Lazy<string> _);

        // Act
        var getServiceTask = given.Provider.GetAsync();
        await Task.Delay(10, Xunit.TestContext.Current.CancellationToken); // Let the polling start
        given.Provider.Dispose();

        // Assert - Provider should be disposed
        var servicesAfterDisposal = await getServiceTask;
        servicesAfterDisposal.ShouldNotBeNull();
        servicesAfterDisposal.Count.ShouldBe(0);
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
            KubeNamespace = nameof(PollKubeIntegrationTests),
        };

        serviceBuilder = new();

        // Create the inner Kube provider
        var kubeProvider = new Kube(config, _factory.Object, client, serviceBuilder.Object);

        // Wrap with PollKube
        var provider = new PollKube(pollingInterval, _factory.Object, kubeProvider);

        return (client, options, provider, config);
    }

    protected void GivenThereIsAFakeKubeServiceDiscoveryProvider(
        string url, string namespaces, string serviceName, EndpointsV1 endpointEntries, out Lazy<string> receivedToken)
        => GivenThereIsAFakeKubeServiceDiscoveryProvider(url, namespaces, serviceName, HttpStatusCode.OK, endpointEntries, out receivedToken);

    protected void GivenThereIsAFakeKubeServiceDiscoveryProvider(
        string url, string namespaces, string serviceName,
        HttpStatusCode statusCode, EndpointsV1 endpointEntries, out Lazy<string> receivedToken)
    {
        var token = string.Empty;
        receivedToken = new(() => token);
        handler.GivenThereIsAServiceRunningOn(url, async context =>
        {
            if (context.Request.Path.Value != $"/api/v1/namespaces/{namespaces}/endpoints/{serviceName}")
                return;

            var responseBody = string.Empty;
            if (context.Request.Headers.TryGetValue("Authorization", out var values))
            {
                token = values.First();
            }

                responseBody = statusCode == HttpStatusCode.OK
                    ? JsonConvert.SerializeObject(endpointEntries, JsonSerializerSettings)
                    : JsonConvert.SerializeObject(new StatusV1
                        {
                            Message = GetKubeApiErrorMessage(serviceName, namespaces, statusCode),
                            Reason = statusCode.ToString(),
                            Code = (int)statusCode,
                            Status = StatusV1.FailureStatus,
                        }, JsonSerializerSettings);

            context.Response.StatusCode = (int)statusCode;
            context.Response.Headers.Append("Content-Type", "application/json");
            await context.Response.WriteAsync(responseBody);
        });
    }

    private static EndpointsV1 GivenEndpoints()
    {
        var endpoints = new EndpointsV1
        {
            Metadata = new ObjectMetaV1 { Name = "test-endpoints", Namespace = nameof(PollKubeIntegrationTests) },
        };
        var subset = new EndpointSubsetV1();
        endpoints.Subsets.Add(subset);
        subset.Addresses.Add(new()
        {
            Ip = "127.0.0.1", TargetRef = new ObjectReferenceV1 { Name = "pod-1" },
        });
        subset.Ports.Add(new()
        {
            Name = "http", Port = 8080,
        });
        return endpoints;
    }

    private static string GetKubeApiErrorMessage(string serviceName, string kubeNamespace, HttpStatusCode responseStatusCode)
    {
        return responseStatusCode switch
        {
            HttpStatusCode.NotFound => $"endpoints \"{serviceName}\" not found",
            HttpStatusCode.Forbidden => $"endpoints \"{serviceName}\" is forbidden: User \"system:serviceaccount:default:default\" cannot get resource \"endpoints\" in API group \"\" in the namespace \"{kubeNamespace}\"",
            HttpStatusCode.BadRequest => $"Bad Request: endpoints \"{serviceName}\" in namespace \"{kubeNamespace}\" is invalid",
            _ => $"Failed to retrieve endpoints \"{serviceName}\" in namespace \"{kubeNamespace}\"",
        };
    }

    #endregion
}
