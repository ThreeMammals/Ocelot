using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Ocelot.ServiceDiscovery;
using System.Reflection;

namespace Ocelot.UnitTests.ServiceDiscovery;

[Trait("PR", "1198")] // https://github.com/ThreeMammals/Ocelot/pull/1198
[Trait("Feat", "1188")] // https://github.com/ThreeMammals/Ocelot/issues/1188
public class MultipleSegmentUrlPathDynamicRouterTests : UnitTest
{
    private MultipleSegmentUrlPathDynamicRouter _routing;
    private readonly Mock<IHttpContextAccessor> contextAccessor = new();

    public MultipleSegmentUrlPathDynamicRouterTests()
    {
        contextAccessor.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());
        _routing = new MultipleSegmentUrlPathDynamicRouter(contextAccessor.Object);
    }

    [Fact]
    public void Delimiter_NoOptions()
    {
        // Arrange, Act
        char actual = (char)DelimiterProperty().GetValue(_routing);

        Assert.Equal(MultipleSegmentUrlPathDynamicRouter.DefaultDelimiter, actual);
    }

    [Fact]
    public void Segments_IsZeroByDefault()
    {
        var actual = SegmentsProperty().GetValue(_routing);

        Assert.Equal(MultipleSegmentUrlPathDynamicRouter.DefaultSegments, actual);
    }

    [Fact]
    public void Delimiter_WithOptions()
    {
        // Arrange, Act
        _routing = new MultipleSegmentUrlPathDynamicRouter(contextAccessor.Object, null);
        char actual = (char)DelimiterProperty().GetValue(_routing);
        Assert.Equal(MultipleSegmentUrlPathDynamicRouter.DefaultDelimiter, actual);

        // Arrange, Act
        var options = Options.Create((MultipleSegmentUrlPathDynamicRouterOptions)null);
        _routing = new MultipleSegmentUrlPathDynamicRouter(contextAccessor.Object, options);
        actual = (char)DelimiterProperty().GetValue(_routing);
        Assert.Equal(MultipleSegmentUrlPathDynamicRouter.DefaultDelimiter, actual);

        // Arrange, Act
        options = Options.Create(new MultipleSegmentUrlPathDynamicRouterOptions('*'));
        _routing = new MultipleSegmentUrlPathDynamicRouter(contextAccessor.Object, options);
        actual = (char)DelimiterProperty().GetValue(_routing);
        Assert.Equal('*', actual);
    }
    private PropertyInfo DelimiterProperty() => _routing.GetType().GetProperty("Delimiter", BindingFlags.NonPublic | BindingFlags.Instance);
    private PropertyInfo SegmentsProperty() => _routing.GetType().GetProperty("Segments", BindingFlags.NonPublic | BindingFlags.Instance);

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
    public void GetServiceName_UsesConfiguredDelimiterWhenDelimiterIsNotOverridden()
    {
        // Arrange
        var options = Options.Create(new MultipleSegmentUrlPathDynamicRouterOptions { Delimiter = '#' });
        var routing = new MultipleSegmentUrlPathDynamicRouter(contextAccessor.Object, options);

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
        var options = Options.Create(new MultipleSegmentUrlPathDynamicRouterOptions('$'));
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

    [Theory]
    [InlineData(true, "%23extra", "/ns.service/api%23/path", "service/api", "ns", "/path")]
    [InlineData(true, "@ignored", "/service/api@path", "service/api", "", "/path")]
    [InlineData(true, "%40%23", "/service/api@path", "service/api", "", "/path")]
    [InlineData(true, "", "/service/api:part", "service/api", "", "/part")]
    [InlineData(true, "%2F", "/service/api:part", "service/api", "", "/part")]
    [InlineData(false, "%23", "/service/api:part", "service/api", "", "/part")]
    public void DelimiterHeader_OverridesConfigurationOnlyWhenEnabled(
        bool isHeaderManagement, string header, string path, string name, string serviceNamespace, string downstream)
    {
        // Arrange
        var options = Options.Create(new MultipleSegmentUrlPathDynamicRouterOptions(':', isHeaderManagement));
        _routing = new MultipleSegmentUrlPathDynamicRouter(contextAccessor.Object, options);
        contextAccessor.Object.HttpContext.Request.Headers[MultipleSegmentUrlPathDynamicRouter.Headers.Ocelot_DynamicRouting_UrlPath_Delimiter] = header;
        contextAccessor.Object.HttpContext.Request.Path = path;

        // Act
        var service = _routing.GetServiceName(path);
        var downstreamPath = _routing.GetDownstreamPath();

        // Assert
        Assert.Equal(name, service.Name);
        Assert.Equal(serviceNamespace, service.Namespace);
        Assert.Equal(downstream, downstreamPath);
    }

    [Theory]
    [InlineData("1", "/ns.service/api:part", "service", "ns", "/api:part")]
    [InlineData("2", "/ns.service/api/path", "service/api", "ns", "/path")]
    [InlineData("2", "/service/api/", "service/api", "", "/")]
    [InlineData("3", "/service/api/path", "service", "", "/api/path")] // fallback
    [InlineData("2147483647", "/service/api/path", "service", "", "/api/path")] // fallback
    [InlineData("0", "/service/api:path", "service/api", "", "/path")]
    [InlineData("-1", "/service/api:path", "service/api", "", "/path")]
    [InlineData("invalid", "/service/api:path", "service/api", "", "/path")]
    [InlineData("2147483648", "/service/api:path", "service/api", "", "/path")]
    [InlineData("0", "/service/api$path", "service", "", "/api$path")] // fallback to 1st segment
    [InlineData("0", "/service/api#path", "service", "", "/api#path")] // fallback to 1st segment
    public void SegmentHeader_SelectsBoundaryOrFallsBack(
        string segments, string path, string name, string serviceNamespace, string downstream)
    {
        // Arrange
        _routing = new MultipleSegmentUrlPathDynamicRouter(contextAccessor.Object,
            Options.Create(new MultipleSegmentUrlPathDynamicRouterOptions('#', true)));
        var request = contextAccessor.Object.HttpContext.Request;
        request.Path = path;
        request.Headers[MultipleSegmentUrlPathDynamicRouter.Headers.Ocelot_DynamicRouting_UrlPath_Segments] = segments;
        request.Headers[MultipleSegmentUrlPathDynamicRouter.Headers.Ocelot_DynamicRouting_UrlPath_Delimiter] = ":";

        // Act
        var service = _routing.GetServiceName(path);
        var downstreamPath = _routing.GetDownstreamPath();

        // Assert
        Assert.Equal(name, service.Name);
        Assert.Equal(serviceNamespace, service.Namespace);
        Assert.Equal(downstream, downstreamPath);
    }

    [Theory]
    [InlineData(1, "/service/api/path", "service", "/api/path")]
    [InlineData(2, "/service/api/path", "service/api", "/path")]
    [InlineData(3, "/service/api:part", "service", "/api:part")]
    [InlineData(0, "/service/api:part", "service/api", "/part")]
    [InlineData(-1, "/service/api:part", "service/api", "/part")]
    public void SegmentsOption_SelectsModeOnlyWhenPositive(
        int segments, string path, string name, string downstream)
    {
        var options = Options.Create(new MultipleSegmentUrlPathDynamicRouterOptions
        {
            Delimiter = ':',
            Segments = segments,
        });
        _routing = new MultipleSegmentUrlPathDynamicRouter(contextAccessor.Object, options);
        contextAccessor.Object.HttpContext.Request.Path = path;

        var service = _routing.GetServiceName();
        var downstreamPath = _routing.GetDownstreamPath();

        Assert.Equal(name, service.Name);
        Assert.Equal(downstream, downstreamPath);
    }

    [Fact]
    public void SegmentsOption_IgnoresDelimiterHeaderInSegmentsMode()
    {
        var options = Options.Create(new MultipleSegmentUrlPathDynamicRouterOptions
        {
            Delimiter = ':',
            IsHeaderManagement = true,
            Segments = 2,
        });
        _routing = new MultipleSegmentUrlPathDynamicRouter(contextAccessor.Object, options);
        var request = contextAccessor.Object.HttpContext.Request;
        request.Headers[MultipleSegmentUrlPathDynamicRouter.Headers.Ocelot_DynamicRouting_UrlPath_Delimiter] = "#";
        request.Path = "/service/api/path#downstream";

        var service = _routing.GetServiceName();
        var downstreamPath = _routing.GetDownstreamPath();

        Assert.Equal("service/api", service.Name);
        Assert.Equal("/path#downstream", downstreamPath);
    }

    [Fact]
    public void OverriddenSegments_TakesPrecedenceOverConfiguredSegments()
    {
        var options = Options.Create(new MultipleSegmentUrlPathDynamicRouterOptions(3)
        {
            IsHeaderManagement = true,
        });
        _routing = new TwoSegmentsRouting(contextAccessor.Object, options);

        var service = _routing.GetServiceName("/service/api/path");
        Assert.Equal("service/api", service.Name);
    }

    [Fact]
    public void SegmentHeader_TakesPrecedenceOverOverriddenSegments()
    {
        var options = Options.Create(new MultipleSegmentUrlPathDynamicRouterOptions(3)
        {
            IsHeaderManagement = true,
        });
        _routing = new TwoSegmentsRouting(contextAccessor.Object, options);
        contextAccessor.Object.HttpContext.Request.Headers[
            MultipleSegmentUrlPathDynamicRouter.Headers.Ocelot_DynamicRouting_UrlPath_Segments] = "1";

        var service = _routing.GetServiceName("/service/api/path");
        Assert.Equal("service", service.Name);
    }

    [Theory]
    [InlineData("2", "service/api", "/path/downstream")]
    [InlineData("-1", "service/api/path", "/downstream")]
    [InlineData("invalid", "service/api/path", "/downstream")]
    [InlineData("2147483648", "service/api/path", "/downstream")]
    [InlineData(null, "service/api/path", "/downstream")]
    public void SegmentHeader_OverridesOrFallsBackToConfiguredSegments(
        string header, string name, string downstream)
    {
        var options = Options.Create(new MultipleSegmentUrlPathDynamicRouterOptions
        {
            IsHeaderManagement = true,
            Segments = 3,
        });
        _routing = new MultipleSegmentUrlPathDynamicRouter(contextAccessor.Object, options);
        var request = contextAccessor.Object.HttpContext.Request;
        if (header != null)
            request.Headers[MultipleSegmentUrlPathDynamicRouter.Headers.Ocelot_DynamicRouting_UrlPath_Segments] = header;
        request.Path = "/service/api/path/downstream";

        var service = _routing.GetServiceName();
        var downstreamPath = _routing.GetDownstreamPath();

        Assert.Equal(name, service.Name);
        Assert.Equal(downstream, downstreamPath);
    }

    [Fact]
    public void ZeroSegmentHeader_SwitchesConfiguredSegmentsModeToDelimiterMode()
    {
        var options = Options.Create(new MultipleSegmentUrlPathDynamicRouterOptions
        {
            Delimiter = ':',
            IsHeaderManagement = true,
            Segments = 2,
        });
        _routing = new MultipleSegmentUrlPathDynamicRouter(contextAccessor.Object, options);
        var request = contextAccessor.Object.HttpContext.Request;
        request.Headers[MultipleSegmentUrlPathDynamicRouter.Headers.Ocelot_DynamicRouting_UrlPath_Segments] = "0";
        request.Path = "/service/api:downstream";

        var service = _routing.GetServiceName();
        var downstreamPath = _routing.GetDownstreamPath();

        Assert.Equal("service/api", service.Name);
        Assert.Equal("/downstream", downstreamPath);
    }

    [Fact]
    public void DisabledHeaderManagement_IgnoresBothHeadersWithoutAccessingHttpContext()
    {
        // Arrange
        contextAccessor.Setup(x => x.HttpContext).Returns((HttpContext)null);
        _routing = new MultipleSegmentUrlPathDynamicRouter(contextAccessor.Object,
            Options.Create(new MultipleSegmentUrlPathDynamicRouterOptions(false)));

        // Act
        var service = _routing.GetServiceName("/service/api$path");

        // Assert
        Assert.Equal("service/api", service.Name);
    }

    [Fact]
    public void DisabledHeaderManagement_IgnoresBothHeaders()
    {
        // Arrange
        var request = contextAccessor.Object.HttpContext.Request;
        request.Path = "/service/api:details";
        request.Headers[MultipleSegmentUrlPathDynamicRouter.Headers.Ocelot_DynamicRouting_UrlPath_Delimiter] = "@";
        request.Headers[MultipleSegmentUrlPathDynamicRouter.Headers.Ocelot_DynamicRouting_UrlPath_Segments] = "1";
        _routing = new MultipleSegmentUrlPathDynamicRouter(contextAccessor.Object,
            Options.Create(new MultipleSegmentUrlPathDynamicRouterOptions(':')));

        // Act
        var service = _routing.GetServiceName();
        var downstreamPath = _routing.GetDownstreamPath();

        // Assert
        Assert.Equal("service/api", service.Name);
        Assert.Equal("/details", downstreamPath);
    }

    [Fact]
    public void EnabledHeader_OverridesVirtualDelimiter()
    {
        // Arrange
        var options = Options.Create(new MultipleSegmentUrlPathDynamicRouterOptions(true));
        _routing = new HashDelimiterRouting(contextAccessor.Object, options);
        contextAccessor.Object.HttpContext.Request.Headers[MultipleSegmentUrlPathDynamicRouter.Headers.Ocelot_DynamicRouting_UrlPath_Delimiter] = ":";

        // Act
        var service = _routing.GetServiceName("/service/api:path");

        // Assert
        Assert.Equal("service/api", service.Name);
    }

    private sealed class HashDelimiterRouting(
        IHttpContextAccessor contextAccessor,
        IOptions<MultipleSegmentUrlPathDynamicRouterOptions> options = null)
        : MultipleSegmentUrlPathDynamicRouter(contextAccessor, options)
    {
        protected override char Delimiter => '#';
    }

    private sealed class SlashDelimiterRouting(IHttpContextAccessor contextAccessor)
        : MultipleSegmentUrlPathDynamicRouter(contextAccessor)
    {
        protected override char Delimiter => Slash;
    }

    private sealed class TwoSegmentsRouting(
        IHttpContextAccessor contextAccessor,
        IOptions<MultipleSegmentUrlPathDynamicRouterOptions> options)
        : MultipleSegmentUrlPathDynamicRouter(contextAccessor, options)
    {
        protected override int Segments => 2;
    }

    private sealed class TestableMultipleSegmentsRouting(IHttpContextAccessor contextAccessor)
        : MultipleSegmentUrlPathDynamicRouter(contextAccessor)
    {
        public string GetDownstreamPathForTest(string upstreamUrlPath)
            => DownstreamPath(upstreamUrlPath);
    }
}
