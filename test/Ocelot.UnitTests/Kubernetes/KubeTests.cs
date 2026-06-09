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
public sealed class KubeTests : IDisposable
{
    private readonly Mock<IOcelotLoggerFactory> _factory = new();
    private readonly Mock<IOcelotLogger> _logger = new();
    private readonly Mock<IKubeServiceBuilder> _serviceBuilder = new();
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

    [Fact]
    [Trait("PR", "14")] // https://github.com/ocelotgateway/Ocelot/pull/14
    public async Task GetAsync_Should_return_empty_list_when_disposed()
    {
        // Arrange
        var mockKubeApi = new Mock<IKubeApiClient>();
        var provider = new Kube(_configuration, _factory.Object, mockKubeApi.Object, _serviceBuilder.Object);

        // Act
        provider.Dispose();
        var result = await provider.GetAsync();

        // Assert - should return empty list when already disposed
        result.ShouldBeEmpty();
    }

    [Fact]
    [Trait("PR", "14")] // https://github.com/ocelotgateway/Ocelot/pull/14
    public void Dispose_Should_release_resources_without_throwing()
    {
        // Arrange
        var mockKubeApi = new Mock<IKubeApiClient>();
        var provider = new Kube(_configuration, _factory.Object, mockKubeApi.Object, _serviceBuilder.Object);

        // Act & Assert - multiple disposes should not throw
        provider.Dispose();
        provider.Dispose(); // Should not throw

        _logger.Verify(x => x.Dispose(), Times.Once);
        mockKubeApi.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    [Trait("PR", "14")] // https://github.com/ocelotgateway/Ocelot/pull/14
    public void Dispose_Should_dispose_logger_and_kubeapi()
    {
        // Arrange
        var mockKubeApi = new Mock<IKubeApiClient>();
        var provider = new Kube(_configuration, _factory.Object, mockKubeApi.Object, _serviceBuilder.Object);

        // Act
        provider.Dispose();

        // Assert
        _logger.Verify(x => x.Dispose(), Times.Once, "Logger should be disposed");
        mockKubeApi.Verify(x => x.Dispose(), Times.Once, "KubeApi should be disposed");
    }

    [Fact]
    [Trait("PR", "14")] // https://github.com/ocelotgateway/Ocelot/pull/14
    public async Task GetAsync_Should_handle_ObjectDisposedException_and_return_empty_list()
    {
        // Arrange
        var mockEndpointClient = new Mock<IEndPointClient>();
        var mockKubeApi = new Mock<IKubeApiClient>();

        // Configure mock to throw ObjectDisposedException every time GetAsync is called
        mockEndpointClient
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ObjectDisposedException("KubeApi", "The API client was disposed"));

        mockKubeApi
            .Setup(x => x.ResourceClient<IEndPointClient>(It.IsAny<Func<IKubeApiClient, IEndPointClient>>()))
            .Returns((Func<IKubeApiClient, IEndPointClient> factory) => factory(mockKubeApi.Object));

        var provider = new Kube(_configuration, _factory.Object, mockKubeApi.Object, _serviceBuilder.Object);

        // Act
        var result = await provider.GetAsync();

        // Assert
        result.ShouldBeEmpty();
        
        // Verify that LogError was called for each retry attempt (3 times total)
        _logger.Verify(
            x => x.LogError(It.IsAny<Func<string>>(), It.IsAny<ObjectDisposedException>()),
            Times.Exactly(3),
            "LogError should be called 3 times for ObjectDisposedException retries");
        
        // Verify that LogWarning was called at the end
        _logger.Verify(
            x => x.LogWarning(It.IsAny<Func<string>>()),
            Times.AtLeastOnce(),
            "LogWarning should be called when no valid result is found");
    }

    [Fact]
    [Trait("PR", "14")] // https://github.com/ocelotgateway/Ocelot/pull/14
    public async Task GetAsync_Should_handle_KubeApiException_and_return_empty_list()
    {
        // Arrange
        var mockEndpointClient = new Mock<IEndPointClient>();
        var mockKubeApi = new Mock<IKubeApiClient>();

        var status = new StatusV1 { Status = "Failure", Reason = "NotFound", Message = "Service not found" };
        var kubeApiException = new KubeApiException("Service not found", status: status);

        // Configure mock to throw KubeApiException every time GetAsync is called
        mockEndpointClient
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(kubeApiException);

        mockKubeApi
            .Setup(x => x.ResourceClient<IEndPointClient>(It.IsAny<Func<IKubeApiClient, IEndPointClient>>()))
            .Returns((Func<IKubeApiClient, IEndPointClient> factory) => factory(mockKubeApi.Object));

        var provider = new Kube(_configuration, _factory.Object, mockKubeApi.Object, _serviceBuilder.Object);

        // Act
        var result = await provider.GetAsync();

        // Assert
        result.ShouldBeEmpty();
        
        // Verify that LogError was called for each retry attempt
        _logger.Verify(
            x => x.LogError(It.IsAny<Func<string>>(), It.IsAny<KubeApiException>()),
            Times.Exactly(3),
            "LogError should be called 3 times for KubeApiException retries");
    }

    [Fact]
    [Trait("PR", "14")] // https://github.com/ocelotgateway/Ocelot/pull/14
    public async Task GetAsync_Should_handle_HttpRequestException_and_return_empty_list()
    {
        // Arrange
        var mockEndpointClient = new Mock<IEndPointClient>();
        var mockKubeApi = new Mock<IKubeApiClient>();

        var httpException = new HttpRequestException("Connection failed", null, System.Net.HttpStatusCode.ServiceUnavailable);

        // Configure mock to throw HttpRequestException every time GetAsync is called
        mockEndpointClient
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(httpException);

        mockKubeApi
            .Setup(x => x.ResourceClient<IEndPointClient>(It.IsAny<Func<IKubeApiClient, IEndPointClient>>()))
            .Returns((Func<IKubeApiClient, IEndPointClient> factory) => factory(mockKubeApi.Object));

        var provider = new Kube(_configuration, _factory.Object, mockKubeApi.Object, _serviceBuilder.Object);

        // Act
        var result = await provider.GetAsync();

        // Assert
        result.ShouldBeEmpty();
        
        // Verify that LogError was called for each retry attempt
        _logger.Verify(
            x => x.LogError(It.IsAny<Func<string>>(), It.IsAny<HttpRequestException>()),
            Times.Exactly(3),
            "LogError should be called 3 times for HttpRequestException retries");
    }

    [Fact]
    [Trait("PR", "14")] // https://github.com/ocelotgateway/Ocelot/pull/14
    public async Task GetAsync_Should_handle_general_Exception_and_return_empty_list()
    {
        // Arrange
        var mockEndpointClient = new Mock<IEndPointClient>();
        var mockKubeApi = new Mock<IKubeApiClient>();

        var unexpectedException = new InvalidOperationException("Unexpected error");

        // Configure mock to throw general Exception every time GetAsync is called
        mockEndpointClient
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(unexpectedException);

        mockKubeApi
            .Setup(x => x.ResourceClient<IEndPointClient>(It.IsAny<Func<IKubeApiClient, IEndPointClient>>()))
            .Returns((Func<IKubeApiClient, IEndPointClient> factory) => factory(mockKubeApi.Object));

        var provider = new Kube(_configuration, _factory.Object, mockKubeApi.Object, _serviceBuilder.Object);

        // Act
        var result = await provider.GetAsync();

        // Assert
        result.ShouldBeEmpty();
        
        // Verify that LogError was called for each retry attempt
        _logger.Verify(
            x => x.LogError(It.IsAny<Func<string>>(), It.IsAny<InvalidOperationException>()),
            Times.Exactly(3),
            "LogError should be called 3 times for general Exception retries");
    }

    [Fact]
    [Trait("PR", "14")] // https://github.com/ocelotgateway/Ocelot/pull/14
    public async Task GetAsync_Should_return_services_when_endpoint_is_valid()
    {
        // Arrange
        var mockEndpointClient = new Mock<IEndPointClient>();
        var mockKubeApi = new Mock<IKubeApiClient>();

        var validEndpoint = new EndpointsV1
        {
            Metadata = new ObjectMetaV1 { Name = "test-service" },
            Subsets = new[]
            {
                new EndpointSubsetV1
                {
                    Addresses = new[]
                    {
                        new EndpointAddressV1 { Ip = "192.168.1.1" }
                    },
                    Ports = new[]
                    {
                        new EndpointPortV1 { Name = "http", Port = 80 }
                    }
                }
            }
        };

        mockEndpointClient
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validEndpoint);

        mockKubeApi
            .Setup(x => x.ResourceClient<IEndPointClient>(It.IsAny<Func<IKubeApiClient, IEndPointClient>>()))
            .Returns((Func<IKubeApiClient, IEndPointClient> factory) => factory(mockKubeApi.Object));

        var expectedServices = new List<Service> { new Service { Name = "test-service", HostAndPort = new HostAndPort("192.168.1.1", 80) } };
        _serviceBuilder
            .Setup(x => x.BuildServices(It.IsAny<KubeRegistryConfiguration>(), It.IsAny<EndpointsV1>()))
            .Returns(expectedServices);

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
    [Trait("PR", "14")] // https://github.com/ocelotgateway/Ocelot/pull/14
    public async Task GetAsync_Should_return_empty_list_when_endpoint_has_no_subsets()
    {
        // Arrange
        var mockEndpointClient = new Mock<IEndPointClient>();
        var mockKubeApi = new Mock<IKubeApiClient>();

        var emptyEndpoint = new EndpointsV1
        {
            Metadata = new ObjectMetaV1 { Name = "test-service" },
            Subsets = null // No subsets
        };

        mockEndpointClient
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyEndpoint);

        mockKubeApi
            .Setup(x => x.ResourceClient<IEndPointClient>(It.IsAny<Func<IKubeApiClient, IEndPointClient>>()))
            .Returns((Func<IKubeApiClient, IEndPointClient> factory) => factory(mockKubeApi.Object));

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
    [Trait("PR", "14")] // https://github.com/ocelotgateway/Ocelot/pull/14
    public async Task BuildServices_Should_return_empty_when_disposed()
    {
        // Arrange
        var mockEndpointClient = new Mock<IEndPointClient>();
        var mockKubeApi = new Mock<IKubeApiClient>();

        var validEndpoint = new EndpointsV1
        {
            Metadata = new ObjectMetaV1 { Name = "test-service" },
            Subsets = new[]
            {
                new EndpointSubsetV1
                {
                    Addresses = new[] { new EndpointAddressV1 { Ip = "192.168.1.1" } },
                    Ports = new[] { new EndpointPortV1 { Name = "http", Port = 80 } }
                }
            }
        };

        mockEndpointClient
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validEndpoint);

        mockKubeApi
            .Setup(x => x.ResourceClient<IEndPointClient>(It.IsAny<Func<IKubeApiClient, IEndPointClient>>()))
            .Returns((Func<IKubeApiClient, IEndPointClient> factory) => factory(mockKubeApi.Object));

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
