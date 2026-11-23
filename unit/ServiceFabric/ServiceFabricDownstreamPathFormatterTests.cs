using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.DownstreamUrlCreator;
using Ocelot.Request.Middleware;
using Ocelot.ServiceFabric;
using Ocelot.Values;

namespace Ocelot.UnitTests.ServiceFabric;

public class ServiceFabricDownstreamPathFormatterTests : UnitTest
{
    private readonly Mock<IDownstreamPathPlaceholderReplacer> _replacer = new();
    private readonly ServiceFabricDownstreamPathFormatter _formatter;

    public ServiceFabricDownstreamPathFormatterTests()
    {
        _formatter = new ServiceFabricDownstreamPathFormatter(_replacer.Object);
    }

    [Fact]
    public void TryFormat_should_create_service_fabric_path()
    {
        // Arrange
        const string serviceName = "FabricService_{version}";
        var placeholders = new List<PlaceholderNameAndValue> { new("version", "1") };
        var downstreamRoute = new DownstreamRouteBuilder()
            .WithServiceName(serviceName)
            .Build();
        var configuration = GivenInternalConfiguration("servicefabric");
        var downstreamRequest = new DownstreamRequest();
        _replacer
            .Setup(x => x.Replace(serviceName, placeholders))
            .Returns(new DownstreamPath("FabricService_1"));

        // Act
        var result = _formatter.TryFormat(
            downstreamRequest,
            downstreamRoute,
            configuration,
            placeholders,
            new DownstreamPath("/api/products"));

        // Assert
        result.ShouldBeTrue();
        downstreamRequest.AbsolutePath.ShouldBe("/FabricService_1/api/products");
        _replacer.Verify(x => x.Replace(serviceName, placeholders), Times.Once);
    }

    [Fact]
    public void TryFormat_should_return_false_when_provider_type_is_not_service_fabric()
    {
        // Arrange
        var downstreamRoute = new DownstreamRouteBuilder()
            .WithServiceName("FabricService")
            .Build();
        var downstreamRequest = new DownstreamRequest();

        // Act
        var result = _formatter.TryFormat(
            downstreamRequest,
            downstreamRoute,
            GivenInternalConfiguration("Consul"),
            Array.Empty<PlaceholderNameAndValue>(),
            new DownstreamPath("/api/products"));

        // Assert
        result.ShouldBeFalse();
        _replacer.VerifyNoOtherCalls();
    }

    [Fact]
    public void TryFormat_should_return_false_when_route_does_not_use_service_discovery()
    {
        // Arrange
        var downstreamRoute = new DownstreamRouteBuilder()
            .WithServiceName(string.Empty)
            .Build();
        var downstreamRequest = new DownstreamRequest();

        // Act
        var result = _formatter.TryFormat(
            downstreamRequest,
            downstreamRoute,
            GivenInternalConfiguration(ServiceFabricServiceDiscoveryProvider.Type),
            Array.Empty<PlaceholderNameAndValue>(),
            new DownstreamPath("/api/products"));

        // Assert
        result.ShouldBeFalse();
        _replacer.VerifyNoOtherCalls();
    }

    private static InternalConfiguration GivenInternalConfiguration(string providerType) => new()
    {
        ServiceProviderConfiguration = new ServiceProviderConfigurationBuilder()
            .WithType(providerType)
            .Build(),
    };
}
