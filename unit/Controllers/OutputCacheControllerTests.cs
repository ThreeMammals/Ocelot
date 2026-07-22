using Microsoft.AspNetCore.Mvc;
using Ocelot.Administration;
using Ocelot.Cache;

namespace Ocelot.UnitTests.Controllers;

public class OutputCacheControllerTests : UnitTest
{
    private readonly OutputCacheController _controller;
    private readonly Mock<IOcelotCache<CachedResponse>> _cache;

    public OutputCacheControllerTests()
    {
        _cache = new();
        _controller = new(_cache.Object);
    }

    [Fact]
    public void Delete_ByKey_ClearedRegion()
    {
        // Arrange, Act
        var result = _controller.Delete("a");

        // Assert
        result.ShouldBeOfType<NoContentResult>();
        _cache.Verify(x => x.ClearRegion("a"), Times.Once);
    }

    [Fact]
    [Trait("Bug", "989")] // https://github.com/ThreeMammals/Ocelot/issues/989
    [Trait("PR", "2412")] // https://github.com/ThreeMammals/Ocelot/pull/2412
    public void Should_have_ignore_api_attribute_to_hide_in_swagger()
    {
        // Arrange, Act
        var attr = Attribute.GetCustomAttribute(typeof(OutputCacheController), typeof(ApiExplorerSettingsAttribute)) as ApiExplorerSettingsAttribute;

        // Assert
        Assert.NotNull(attr);
        Assert.True(attr.IgnoreApi);
    }
}
