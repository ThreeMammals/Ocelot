using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Ocelot.ServiceDiscovery;
using System.Reflection;

namespace Ocelot.UnitTests.ServiceDiscovery;

[Trait("PR", "1198")] // https://github.com/ThreeMammals/Ocelot/pull/1198
[Trait("Feat", "1188")] // https://github.com/ThreeMammals/Ocelot/issues/1188
public class MultipleSegmentUrlPathDynamicRoutingTests : UnitTest
{
    private MultipleSegmentUrlPathDynamicRouting _routing;
    private readonly Mock<IHttpContextAccessor> contextAccessor = new();

    public MultipleSegmentUrlPathDynamicRoutingTests()
    {
        contextAccessor.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());
        _routing = new MultipleSegmentUrlPathDynamicRouting(contextAccessor.Object);
    }

    [Fact]
    public void ServiceNameDelimiter_NoOptions()
    {
        // Arrange, Act
        char actual = (char)ServiceNameDelimiter().GetValue(_routing);

        Assert.Equal(MultipleSegmentUrlPathDynamicRouting.DefaultDelimiter, actual);
    }

    [Fact]
    public void ServiceNameDelimiter_WithOptions()
    {
        // Arrange, Act
        _routing = new MultipleSegmentUrlPathDynamicRouting(contextAccessor.Object, null);
        char actual = (char)ServiceNameDelimiter().GetValue(_routing);
        Assert.Equal(MultipleSegmentUrlPathDynamicRouting.DefaultDelimiter, actual);

        // Arrange, Act
        var options = Options.Create((MultipleSegmentUrlPathDynamicRoutingOptions)null);
        _routing = new MultipleSegmentUrlPathDynamicRouting(contextAccessor.Object, options);
        actual = (char)ServiceNameDelimiter().GetValue(_routing);
        Assert.Equal(MultipleSegmentUrlPathDynamicRouting.DefaultDelimiter, actual);

        // Arrange, Act
        options = Options.Create(new MultipleSegmentUrlPathDynamicRoutingOptions('*'));
        _routing = new MultipleSegmentUrlPathDynamicRouting(contextAccessor.Object, options);
        actual = (char)ServiceNameDelimiter().GetValue(_routing);
        Assert.Equal('*', actual);
    }
    private PropertyInfo ServiceNameDelimiter() => _routing.GetType().GetProperty(nameof(ServiceNameDelimiter), BindingFlags.NonPublic | BindingFlags.Instance);

    [Theory]
    [InlineData("/authorization$path", "authorization", "/path")]
    [InlineData("/authorization$/path", "authorization", "/path")]
    [InlineData("/authorization/$/path", "authorization", "/path")]
    [InlineData("/authorization/api/user$/path", "authorization/api/user", "/path")]
    [InlineData("/authorization/api/user$path", "authorization/api/user", "/path")]
    [InlineData("/authorization/api/user/$/path", "authorization/api/user", "/path")]
    [InlineData("/authorization/api/user$", "authorization/api/user", "/")]
    [InlineData("/namespace.authorization/api$/path", "authorization/api", "/path", "namespace")]
    [InlineData("/namespace.authorization$path", "authorization", "/path", "namespace")]
    public void GetServiceName_UsesDelimiterToSplitServiceNameAndDownstreamPath(
        string upstreamPath, string expectedServiceName, string expectedDownstreamPath, string expectedNamespace = "")
    {
        // Arrange
        contextAccessor.Object.HttpContext.Request.Path = upstreamPath;

        // Act
        var actualServiceName = _routing.GetServiceName(upstreamPath);
        var actualDownstreamPath = _routing.GetDownstreamPath();

        // Assert
        Assert.Equal(expectedServiceName, actualServiceName.Name);
        Assert.Equal(expectedNamespace, actualServiceName.Namespace);
        Assert.Equal(expectedDownstreamPath, actualDownstreamPath);
    }

    [Fact]
    public void GetServiceName_UsesBaseRoutingWithoutDelimiter()
    {
        // Arrange
        contextAccessor.Object.HttpContext.Request.Path = "/authorization/api";

        // Act
        var actualServiceName = _routing.GetServiceName();
        var actualDownstreamPath = _routing.GetDownstreamPath();

        // Assert
        Assert.Equal("authorization", actualServiceName.Name);
        Assert.Equal("/api", actualDownstreamPath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/")]
    public void GetServiceName_WithEmptyPathUsesRootPath(string urlPath)
    {
        // Arrange
        var routing = new TestableMultipleSegmentsRouting(contextAccessor.Object);

        // Act
        var actualServiceName = routing.GetServiceName(urlPath);
        var actualDownstreamPath = routing.GetDownstreamPathForTest(urlPath);

        // Assert
        Assert.Equal(string.Empty, actualServiceName.Name);
        Assert.Equal(string.Empty, actualServiceName.Namespace);
        Assert.Equal("/", actualDownstreamPath);
    }

    [Fact]
    public void GetServiceName_UsesConfiguredDelimiterWhenServiceNameDelimiterIsNotOverridden()
    {
        // Arrange
        var options = Options.Create(new MultipleSegmentUrlPathDynamicRoutingOptions { Delimiter = '#' });
        var routing = new MultipleSegmentUrlPathDynamicRouting(contextAccessor.Object, options);

        // Act
        var actualServiceName = routing.GetServiceName("/authorization/api#path");

        // Assert
        Assert.Equal("authorization/api", actualServiceName.Name);
    }

    [Fact]
    public void GetServiceName_ReplacesSlashCustomDelimiterWithDefault()
    {
        // Arrange
        var routing = new SlashDelimiterRouting(contextAccessor.Object);

        // Act
        var actual = routing.GetServiceName("/authorization/api$path");

        // Assert
        Assert.Equal("authorization/api", actual.Name);
    }

    [Fact]
    public void GetServiceName_UsesDefaultDelimiterWithoutOptions()
    {
        // Arrange, Act
        var actual = _routing.GetServiceName("/authorization/api$path");

        // Assert
        Assert.Equal("authorization/api", actual.Name);
    }

    [Fact]
    public void GetServiceName_UsesOverriddenDelimiterInsteadOfConfiguredDelimiter()
    {
        // Arrange
        var options = Options.Create(new MultipleSegmentUrlPathDynamicRoutingOptions('$'));
        var routing = new HashDelimiterRouting(contextAccessor.Object, options);

        // Act, Assert
        var actual = routing.GetServiceName("/authorization/api#path"); // overridden delimiter is #
        Assert.Equal("authorization/api", actual.Name);

        // Act, Assert
        actual = routing.GetServiceName("/authorization/api$path"); // options delimiter is $ for base class
        Assert.Equal("authorization", actual.Name);
    }

    [Theory]
    [InlineData("/authorization/api/user%23", "authorization/api/user", "/")]
    [InlineData("/authorization/api/user%23path", "authorization/api/user", "/path")]
    [InlineData("/namespace.authorization/api%23/path", "authorization/api", "/path", "namespace")]
    [InlineData("/namespace.authorization/api%23/path#section", "authorization/api", "/path#section", "namespace")]
    public void GetServiceName_UsesEncodedHashDelimiter_PathDecoded(
        string upstreamPath, string serviceName, string downstreamPath, string @namespace = "")
    {
        // Arrange
        contextAccessor.Object.HttpContext.Request.Path = upstreamPath;
        _routing = new HashDelimiterRouting(contextAccessor.Object);

        // Act
        var actual = _routing.GetServiceName(upstreamPath);
        var actualPath = _routing.GetDownstreamPath();

        // Assert
        Assert.Equal(serviceName, actual.Name);
        Assert.Equal(@namespace, actual.Namespace);
        Assert.Equal(downstreamPath, actualPath);
    }

    private sealed class HashDelimiterRouting(
        IHttpContextAccessor contextAccessor,
        IOptions<MultipleSegmentUrlPathDynamicRoutingOptions> options = null)
        : MultipleSegmentUrlPathDynamicRouting(contextAccessor, options)
    {
        protected override char ServiceNameDelimiter => '#';
    }

    private sealed class SlashDelimiterRouting(IHttpContextAccessor contextAccessor)
        : MultipleSegmentUrlPathDynamicRouting(contextAccessor)
    {
        protected override char ServiceNameDelimiter => Slash;
    }

    private sealed class TestableMultipleSegmentsRouting(IHttpContextAccessor contextAccessor)
        : MultipleSegmentUrlPathDynamicRouting(contextAccessor)
    {
        public string GetDownstreamPathForTest(string upstreamUrlPath)
            => DownstreamPath(upstreamUrlPath);
    }
}
