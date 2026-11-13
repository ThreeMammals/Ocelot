using Ocelot.ServiceDiscovery;

namespace Ocelot.UnitTests.ServiceDiscovery;

public class ServiceInfoTests : UnitTest
{
    [Fact]
    public void StringConstructor_UsesServiceNameAndEmptyNamespace()
    {
        var actual = new ServiceInfo("orders");

        Assert.Equal("orders", actual.Name);
        Assert.Empty(actual.Namespace);
        Assert.Equal("orders", actual.ToString());
    }

    [Fact]
    public void SpanConstructor_UsesServiceNameAndEmptyNamespace()
    {
        ReadOnlySpan<char> serviceName = "orders";

        var actual = new ServiceInfo(serviceName);

        Assert.Equal("orders", actual.Name);
        Assert.Empty(actual.Namespace);
    }

    [Fact]
    public void StringConstructor_WithNamespace_UsesBothValues()
    {
        var actual = new ServiceInfo("orders", "catalog");

        Assert.Equal("orders", actual.Name);
        Assert.Equal("catalog", actual.Namespace);
        Assert.Equal("catalog.orders", actual.ToString());
    }

    [Fact]
    public void SpanConstructor_WithNamespace_UsesBothValues()
    {
        ReadOnlySpan<char> serviceName = "orders";
        ReadOnlySpan<char> serviceNamespace = "catalog";

        var actual = new ServiceInfo(serviceName, serviceNamespace);

        Assert.Equal("orders", actual.Name);
        Assert.Equal("catalog", actual.Namespace);
    }

    [Fact]
    public void CopyConstructor_CopiesValues()
    {
        var original = new ServiceInfo("orders", "catalog");

        var actual = new ServiceInfo(original);

        Assert.Equal(original.Name, actual.Name);
        Assert.Equal(original.Namespace, actual.Namespace);
    }
}
