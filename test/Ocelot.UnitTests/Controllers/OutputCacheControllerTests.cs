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
}
