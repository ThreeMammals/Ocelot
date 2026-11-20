using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Ocelot.ServiceDiscovery;

namespace Ocelot.UnitTests.ServiceDiscovery;

[Trait("PR", "1198")]
[Trait("Feat", "1188")]
public class MultipleSegmentUrlPathDynamicRoutingTests : UnitTest
{
    private readonly Mock<IHttpContextAccessor> contextAccessor = new();

    public MultipleSegmentUrlPathDynamicRoutingTests()
    {
        contextAccessor.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());
    }

    [Theory]
    [InlineData("/authorization/api/user$/details", "authorization/api/user", "/details")]
    [InlineData("/authorization/api/user$details", "authorization/api/user", "/details")]
    [InlineData("/authorization/api/user/$/details", "authorization/api/user", "/details")]
    [InlineData("/authorization/api/user$", "authorization/api/user", "/")]
    [InlineData("/namespace.authorization/api$/details", "authorization/api", "/details", "namespace")]
    public void UsesDelimiterToSplitServiceNameAndDownstreamPath(
        string upstreamPath, string expectedServiceName, string expectedDownstreamPath,
        string expectedNamespace = "")
    {
        // Arrange
        var routing = new MultipleSegmentUrlPathDynamicRouting(contextAccessor.Object);
        contextAccessor.Object.HttpContext.Request.Path = upstreamPath;

        // Act
        var actualServiceName = routing.GetServiceName(upstreamPath);
        var actualDownstreamPath = routing.GetDownstreamPath();

        // Assert
        Assert.Equal(expectedServiceName, actualServiceName.Name);
        Assert.Equal(expectedNamespace, actualServiceName.Namespace);
        Assert.Equal(expectedDownstreamPath, actualDownstreamPath);
    }

    [Fact]
    public void UsesBaseRoutingWithoutDelimiter()
    {
        // Arrange
        var routing = new MultipleSegmentUrlPathDynamicRouting(contextAccessor.Object);
        contextAccessor.Object.HttpContext.Request.Path = "/authorization/api";

        // Act
        var actualServiceName = routing.GetServiceName();
        var actualDownstreamPath = routing.GetDownstreamPath();

        // Assert
        Assert.Equal("authorization", actualServiceName.Name);
        Assert.Equal("/api", actualDownstreamPath);
    }

    [Fact]
    public void WithNullPathUsesRootPath()
    {
        // Arrange
        var routing = new TestableMultipleSegmentsRouting(contextAccessor.Object);

        // Act
        var actualServiceName = routing.GetServiceName(null);
        var actualDownstreamPath = routing.GetDownstreamPathForTest(null);

        // Assert
        Assert.Equal(string.Empty, actualServiceName.Name);
        Assert.Equal(string.Empty, actualServiceName.Namespace);
        Assert.Equal("/", actualDownstreamPath);
    }

    [Fact]
    [Trait("PR", "1198")]
    [Trait("Feat", "1188")]
    public void UsesConfiguredDelimiterWhenServiceNameDelimiterIsNotOverridden()
    {
        // Arrange
        var options = Options.Create(new MultipleSegmentUrlPathDynamicRoutingOptions { Delimiter = '#' });
        var routing = new MultipleSegmentUrlPathDynamicRouting(contextAccessor.Object, options);

        // Act
        var actualServiceName = routing.GetServiceName("/authorization/api#details");

        // Assert
        Assert.Equal("authorization/api", actualServiceName.Name);
    }

    [Fact]
    public void ReplacesSlashCustomDelimiterWithDefault()
    {
        // Arrange
        var routing = new SlashDelimiterRouting(contextAccessor.Object);

        // Act
        var actual = routing.GetServiceName("/authorization/api$details");

        // Assert
        Assert.Equal("authorization/api", actual.Name);
    }

    [Fact]
    [Trait("PR", "1198")]
    [Trait("Feat", "1188")]
    public void UsesDefaultDelimiterWithoutOptions()
    {
        // Arrange
        var routing = new MultipleSegmentUrlPathDynamicRouting(contextAccessor.Object);

        // Act
        var actual = routing.GetServiceName("/authorization/api$details");

        // Assert
        Assert.Equal("authorization/api", actual.Name);
    }

    [Fact]
    [Trait("PR", "1198")]
    [Trait("Feat", "1188")]
    public void UsesOverriddenDelimiterInsteadOfConfiguredDelimiter()
    {
        // Arrange
        var options = Options.Create(new MultipleSegmentUrlPathDynamicRoutingOptions { Delimiter = '$' });
        var routing = new CustomDelimiterRouting(contextAccessor.Object, options);

        // Act
        var actual = routing.GetServiceName("/authorization/api#details");

        // Assert
        Assert.Equal("authorization/api", actual.Name);
    }

    private sealed class CustomDelimiterRouting(
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
