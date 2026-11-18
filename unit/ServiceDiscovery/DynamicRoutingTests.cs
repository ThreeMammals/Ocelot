
using Ocelot.ServiceDiscovery;

namespace Ocelot.UnitTests.ServiceDiscovery;

public class DynamicRoutingTests : UnitTest
{
    [Theory]
    [InlineData("/", "orders", null, "/orders/")]
    [InlineData("/", "orders", "catalog", "/catalog.orders/")]
    [InlineData("/api/v1", "orders", "catalog", "/catalog.orders/api/v1")]
    public void Path_Should_Build_Upstream_Path_With_ServiceNamespace_And_ServiceName(
        string path, string serviceName, string serviceNamespace, string expected)
    {
        // Act
        var actual = DynamicRouting.Path(path, serviceName, serviceNamespace);

        // Assert
        Assert.Equal(expected, actual);
    }
}
