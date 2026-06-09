using KubeClient;
using KubeClient.Models;
using Ocelot.Logging;
using Ocelot.Provider.Kubernetes;
using Ocelot.Provider.Kubernetes.Interfaces;
using Ocelot.Values;

namespace Ocelot.UnitTests.Kubernetes;

/// <summary>
/// Tests for Kube service discovery provider error handling, particularly around 
/// ObjectDisposedException scenarios that occur during shutdown.
/// </summary>
[Trait("Milestone", ".NET 10")]
[Trait("Release", "25.0.0")]
[Trait("PR", "14")] // https://github.com/ocelotgateway/Ocelot/pull/14
public sealed class KubeTests : IDisposable
{
    private readonly Mock<IOcelotLoggerFactory> _factory = new();
    private readonly Mock<IOcelotLogger> _logger = new();
    private readonly Mock<IKubeServiceBuilder> _serviceBuilder = new();
    private readonly Mock<IEndPointClient> _endpointClient = new();
    private readonly KubeRegistryConfiguration _configuration;

    public KubeTests()
    {
        _factory.Setup(x => x.CreateLogger<Kube>()).Returns(_logger.Object);
        _configuration = new KubeRegistryConfiguration
        {
            KeyOfServiceInK8s = "test-service",
            KubeNamespace = "default"
        };
    }

    public void Dispose()
    {
        _logger?.Invocations.Clear();
    }

    private Mock<IKubeApiClient> CreateMockKubeApi()
    {
        var mockKubeApi = new Mock<IKubeApiClient>();
        mockKubeApi
            .Setup(x => x.ResourceClient<IEndPointClient>(It.IsAny<Func<IKubeApiClient, IEndPointClient>>()))
            .Returns((Func<IKubeApiClient, IEndPointClient> factory) => _endpointClient.Object);
        return mockKubeApi;
    }

    [Fact]
    public async Task GetAsync_Should_return_empty_list_when_disposed()
    {
        // Arrange
        var mockKubeApi = CreateMockKubeApi();
        var provider = new Kube(_configuration, _factory.Object, mockKubeApi.Object, _serviceBuilder.Object);

        // Act
        provider.Dispose();
        var result = await provider.GetAsync();

        // Assert - should return empty list when already disposed
        result.ShouldBeEmpty();
    }

    [Fact]
    public void Dispose_Should_release_resources_without_throwing()
    {
        // Arrange
        var mockKubeApi = CreateMockKubeApi();
        var provider = new Kube(_configuration, _factory.Object, mockKubeApi.Object, _serviceBuilder.Object);

        // Act & Assert - multiple disposes should not throw
        provider.Dispose();
        provider.Dispose(); // Should not throw

        _logger.Verify(x => x.Dispose(), Times.Once);
        mockKubeApi.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_Should_dispose_logger_and_kubeapi()
    {
        // Arrange
        var mockKubeApi = CreateMockKubeApi();
        var provider = new Kube(_configuration, _factory.Object, mockKubeApi.Object, _serviceBuilder.Object);

        // Act
        provider.Dispose();

        // Assert
        _logger.Verify(x => x.Dispose(), Times.Once, "Logger should be disposed");
        mockKubeApi.Verify(x => x.Dispose(), Times.Once, "KubeApi should be disposed");
    }

    [Fact]
    public async Task GetAsync_Should_handle_ObjectDisposedException_and_return_empty_list()
    {
        // Arrange
        _endpointClient
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ObjectDisposedException("KubeApi", "The API client was disposed"));

        var mockKubeApi = CreateMockKubeApi();
        var provider = new Kube(_configuration, _factory.Object, mockKubeApi.Object, _serviceBuilder.Object);

        // Act
        var result = await provider.GetAsync();

        // Assert
        result.ShouldBeEmpty();
        
        // Verify that LogError was called for the exception
        _logger.Verify(
            x => x.LogError(It.IsAny<Func<string>>(), It.IsAny<ObjectDisposedException>()),
            Times.AtLeastOnce(),
            "LogError should be called for ObjectDisposedException");
        
        // Verify that LogWarning was called when no valid result is found
        _logger.Verify(
            x => x.LogWarning(It.IsAny<Func<string>>()),
            Times.AtLeastOnce(),
            "LogWarning should be called when no valid result is found");
    }

    [Fact]
    public async Task GetAsync_Should_handle_KubeApiException_and_return_empty_list()
    {
        // Arrange
        var status = new StatusV1 { Status = "Failure", Reason = "NotFound", Message = "Service not found" };
        var kubeApiException = new KubeApiException("Service not found", status: status);

        _endpointClient
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(kubeApiException);

        var mockKubeApi = CreateMockKubeApi();
        var provider = new Kube(_configuration, _factory.Object, mockKubeApi.Object, _serviceBuilder.Object);

        // Act
        var result = await provider.GetAsync();

        // Assert
        result.ShouldBeEmpty();
        
        // Verify that LogError was called for the exception
        _logger.Verify(
            x => x.LogError(It.IsAny<Func<string>>(), It.IsAny<KubeApiException>()),
            Times.AtLeastOnce(),
            "LogError should be called for KubeApiException");
    }

    [Fact]
    public async Task GetAsync_Should_handle_HttpRequestException_and_return_empty_list()
    {
        // Arrange
        var httpException = new HttpRequestException("Connection failed", null, System.Net.HttpStatusCode.ServiceUnavailable);

        _endpointClient
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(httpException);

        var mockKubeApi = CreateMockKubeApi();
        var provider = new Kube(_configuration, _factory.Object, mockKubeApi.Object, _serviceBuilder.Object);

        // Act
        var result = await provider.GetAsync();

        // Assert
        result.ShouldBeEmpty();
        
        // Verify that LogError was called for the exception
        _logger.Verify(
            x => x.LogError(It.IsAny<Func<string>>(), It.IsAny<HttpRequestException>()),
            Times.AtLeastOnce(),
            "LogError should be called for HttpRequestException");
    }

    [Fact]
    public async Task GetAsync_Should_handle_general_Exception_and_return_empty_list()
    {
        // Arrange
        var unexpectedException = new InvalidOperationException("Unexpected error");

        _endpointClient
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(unexpectedException);

        var mockKubeApi = CreateMockKubeApi();
        var provider = new Kube(_configuration, _factory.Object, mockKubeApi.Object, _serviceBuilder.Object);

        // Act
        var result = await provider.GetAsync();

        // Assert
        result.ShouldBeEmpty();
        
        // Verify that LogError was called for the exception
        _logger.Verify(
            x => x.LogError(It.IsAny<Func<string>>(), It.IsAny<InvalidOperationException>()),
            Times.AtLeastOnce(),
            "LogError should be called for general Exception");
    }

    [Fact]
    public async Task GetAsync_Should_return_services_when_endpoint_is_valid()
    {
        // Arrange
        // Create a valid endpoint with subsets
        var validEndpoint = new EndpointsV1
        {
            Metadata = new ObjectMetaV1 { Name = "test-service" }
        };
        var subset = new EndpointSubsetV1();
        subset.Addresses.Add(new EndpointAddressV1 { Ip = "192.168.1.1" });
        subset.Ports.Add(new EndpointPortV1 { Port = 80 });
        validEndpoint.Subsets.Add(subset);

        _endpointClient
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validEndpoint);

        var expectedServices = new List<Service> { new("test-service", new("192.168.1.1", 80), string.Empty, string.Empty, Array.Empty<string>()) };
        _serviceBuilder
            .Setup(x => x.BuildServices(It.IsAny<KubeRegistryConfiguration>(), It.IsAny<EndpointsV1>()))
            .Returns(expectedServices);

        var mockKubeApi = CreateMockKubeApi();
        var provider = new Kube(_configuration, _factory.Object, mockKubeApi.Object, _serviceBuilder.Object);

        // Act
        var result = await provider.GetAsync();

        // Assert
        result.ShouldNotBeEmpty();
        _serviceBuilder.Verify(
            x => x.BuildServices(It.IsAny<KubeRegistryConfiguration>(), It.IsAny<EndpointsV1>()),
            Times.Once,
            "BuildServices should be called once with valid endpoint");
    }

    [Fact]
    public async Task GetAsync_Should_return_empty_list_when_endpoint_has_no_subsets()
    {
        // Arrange
        // Create an endpoint with no subsets (empty collection)
        var emptyEndpoint = new EndpointsV1
        {
            Metadata = new ObjectMetaV1 { Name = "test-service" }
            // Subsets are empty by default
        };

        _endpointClient
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyEndpoint);

        var mockKubeApi = CreateMockKubeApi();
        var provider = new Kube(_configuration, _factory.Object, mockKubeApi.Object, _serviceBuilder.Object);

        // Act
        var result = await provider.GetAsync();

        // Assert
        result.ShouldBeEmpty();
        _serviceBuilder.Verify(
            x => x.BuildServices(It.IsAny<KubeRegistryConfiguration>(), It.IsAny<EndpointsV1>()),
            Times.Never,
            "BuildServices should not be called when endpoint has no subsets");
    }

    [Fact]
    public async Task BuildServices_Should_return_empty_when_disposed()
    {
        // Arrange
        // Create a valid endpoint with subsets
        var validEndpoint = new EndpointsV1
        {
            Metadata = new ObjectMetaV1 { Name = "test-service" }
        };
        var subset = new EndpointSubsetV1();
        subset.Addresses.Add(new EndpointAddressV1 { Ip = "192.168.1.1" });
        subset.Ports.Add(new EndpointPortV1 { Port = 80 });
        validEndpoint.Subsets.Add(subset);

        _endpointClient
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validEndpoint);

        var mockKubeApi = CreateMockKubeApi();
        var provider = new Kube(_configuration, _factory.Object, mockKubeApi.Object, _serviceBuilder.Object);
        provider.Dispose();

        // Act
        var result = await provider.GetAsync();

        // Assert
        result.ShouldBeEmpty();
        _serviceBuilder.Verify(
            x => x.BuildServices(It.IsAny<KubeRegistryConfiguration>(), It.IsAny<EndpointsV1>()),
            Times.Never,
            "BuildServices should not be called when provider is disposed");
    }
}
