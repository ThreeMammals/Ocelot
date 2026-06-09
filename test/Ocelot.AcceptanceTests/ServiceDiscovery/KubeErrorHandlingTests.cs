using KubeClient;
using KubeClient.Models;
using Ocelot.Logging;
using Ocelot.Provider.Kubernetes;
using Ocelot.Provider.Kubernetes.Interfaces;
using Ocelot.Values;

namespace Ocelot.AcceptanceTests.ServiceDiscovery;

/// <summary>
/// Tests for Kube service discovery provider error handling, particularly around 
/// ObjectDisposedException scenarios that occur during shutdown.
/// </summary>
[Trait("Milestone", ".NET 10")]
[Trait("Release", "25.0.0")]
public class KubeErrorHandlingTests : IDisposable
{
    private readonly Mock<IOcelotLoggerFactory> _factory = new();
    private readonly Mock<IOcelotLogger> _logger = new();
    private readonly Mock<IKubeServiceBuilder> _serviceBuilder = new();
    private readonly KubeRegistryConfiguration _configuration;

    public KubeErrorHandlingTests()
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
}
