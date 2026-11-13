using Ocelot.ServiceDiscovery;

namespace Ocelot.UnitTests.ServiceDiscovery;

[Trait("PR", "1198")]
[Trait("Feat", "1188")]
public class ServiceInfoTests : UnitTest
{
    [Fact]
    public void StringConstructor_UsesServiceNameAndEmptyNamespace()
    {
        // Act
        var actual = new ServiceInfo("orders");

        // Assert
        Assert.Equal("orders", actual.Name);
        Assert.Empty(actual.Namespace);
        Assert.Equal("orders", actual.ToString());
    }

    [Fact]
    public void SpanConstructor_UsesServiceNameAndEmptyNamespace()
    {
        // Arrange
        ReadOnlySpan<char> serviceName = "orders";

        // Act
        var actual = new ServiceInfo(serviceName);

        // Assert
        Assert.Equal("orders", actual.Name);
        Assert.Empty(actual.Namespace);
    }

    [Fact]
    public void StringConstructor_WithNamespace_UsesBothValues()
    {
        // Act
        var actual = new ServiceInfo("orders", "catalog");

        // Assert
        Assert.Equal("orders", actual.Name);
        Assert.Equal("catalog", actual.Namespace);
        Assert.Equal("catalog.orders", actual.ToString());
    }

    [Fact]
    public void SpanConstructor_WithNamespace_UsesBothValues()
    {
        // Arrange
        ReadOnlySpan<char> serviceName = "orders";
        ReadOnlySpan<char> serviceNamespace = "catalog";

        // Act
        var actual = new ServiceInfo(serviceName, serviceNamespace);

        // Assert
        Assert.Equal("orders", actual.Name);
        Assert.Equal("catalog", actual.Namespace);
    }
}
