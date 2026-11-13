
using Microsoft.AspNetCore.Http;
using Ocelot.ServiceDiscovery;

namespace Ocelot.UnitTests.ServiceDiscovery;

public class UrlPathDynamicRoutingTests : UnitTest
{
    private readonly UrlPathDynamicRouting sut;
    private readonly Mock<IHttpContextAccessor> contextAccessor = new();
    public UrlPathDynamicRoutingTests()
    {
        contextAccessor.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());
        sut = new(contextAccessor.Object);
    }

    [Theory]
    [InlineData("/", "orders", null, "/orders/")]
    [InlineData("/", "orders", "catalog", "/catalog.orders/")]
    [InlineData("/api/v1", "orders", "catalog", "/catalog.orders/api/v1")]
    public void Path_Should_Build_Upstream_Path_With_ServiceNamespace_And_ServiceName(
        string path, string serviceName, string serviceNamespace, string expected)
    {
        // Act
        var actual = UrlPathDynamicRouting.UpstreamPath(path, serviceName, serviceNamespace);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [Trait("PR", "2324")]
    [InlineData("/service1", "service1", "")] // edge case -> empty namespace
    [InlineData("/service2/", "service2", "")] // edge case -> empty namespace
    [InlineData("/service3/bla", "service3", "")] // edge case -> empty namespace
    [InlineData("/namespace1.service1", "service1", "namespace1")]
    [InlineData("/namespace2.service2/", "service2", "namespace2")]
    [InlineData("/namespace3.service3/bla-bla", "service3", "namespace3")]
    [InlineData("/namespace4.service.4/ha-a", "service.4", "namespace4")]
    [InlineData("/name.space5.service5/ha-ha", "space5.service5", "name")]
    [InlineData("/.service999/everything", "service999", "")] // edge case -> empty namespace
    [InlineData("/.service.999/everything", "service.999", "")] // edge case -> empty namespace
    public void GetServiceName_ReturnsNamespace(string urlPath, string expected, string expectedNamespace)
    {
        // Act
        var actual = sut.GetServiceName(urlPath);

        Assert.Equal(expected, actual.Name);
        Assert.Equal(expectedNamespace, actual.Namespace);
    }

    [Theory]
    [Trait("PR", "1198")]
    [InlineData(null, "", "")]
    [InlineData("", "", "")]
    [InlineData("/", "", "")]
    public void GetServiceName_EmptyUrl_ReturnsEmpty(string urlPath, string expected, string expectedNamespace)
    {
        // Act
        var actual = sut.GetServiceName(urlPath);

        Assert.Equal(expected, actual.Name);
        Assert.Equal(expectedNamespace, actual.Namespace);
    }

    [Fact]
    public void Request_ReturnsCurrentHttpRequest()
    {
        // Act
        var actual = sut.Request;

        Assert.Same(contextAccessor.Object.HttpContext.Request, actual);
    }

    [Theory]
    [InlineData("/orders/api", "/api")]
    [InlineData("/orders", "/")]
    public void GetDownstreamPath_ReturnsPathWithoutServiceName(string upstreamPath, string expected)
    {
        contextAccessor.Object.HttpContext.Request.Path = upstreamPath;

        // Act
        var actual = sut.GetDownstreamPath();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetDownstreamPath_WithRequest_ReturnsPathWithoutServiceName()
    {
        var request = new DefaultHttpContext().Request;
        request.Path = "/catalog.orders/api";

        // Act
        var actual = sut.GetDownstreamPath(request);

        Assert.Equal("/api", actual);
    }

    [Fact]
    public void GetServiceName_WithoutPathArgument_UsesCurrentRequest()
    {
        contextAccessor.Object.HttpContext.Request.Path = "/catalog.orders/api";

        // Act
        var actual = sut.GetServiceName();

        Assert.Equal("orders", actual.Name);
        Assert.Equal("catalog", actual.Namespace);
    }

    [Fact]
    public void SetServiceName_ReturnsPathWithServiceName()
    {
        // Act
        var actual = sut.SetServiceName("/api", new ServiceInfo("orders", "catalog"));

        Assert.Equal("/catalog.orders/api", actual);
    }

    [Fact]
    public void UpstreamPath_WithNullPath_UsesRootPath()
    {
        // Act
        var actual = UrlPathDynamicRouting.UpstreamPath(null, "orders");

        Assert.Equal("/orders/", actual);
    }
}
