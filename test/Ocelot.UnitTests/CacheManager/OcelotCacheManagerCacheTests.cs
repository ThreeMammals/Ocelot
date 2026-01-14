using CacheManager.Core;
using Ocelot.Cache.CacheManager;

namespace Ocelot.UnitTests.CacheManager;

public class OcelotCacheManagerCacheTests : UnitTest
{
    private readonly Mock<ICacheManager<string>> _manager;
    private readonly OcelotCacheManagerCache<string> _cacheManager;
    public OcelotCacheManagerCacheTests()
    {
        _manager = new Mock<ICacheManager<string>>();
        _cacheManager = new OcelotCacheManagerCache<string>(_manager.Object);
    }

    protected static readonly TimeSpan TTL = TimeSpan.FromSeconds(1);

    [Fact]
    public void Add()
    {
        // Arrange, Act
        _cacheManager.Add("someKey", "someValue", "region", TTL);

        // Assert
        _manager.Verify(x => x.Add(It.IsAny<CacheItem<string>>()), Times.Once);
    }

    [Fact]
    public void AddOrUpdate()
    {
        // Arrange
        _manager.Setup(x => x.AddOrUpdate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<string, string>>()))
            .Returns("someValue");

        // Act
        var actual = _cacheManager.AddOrUpdate("someKey", "someValue", "region", TTL);

        // Assert
        _manager.Verify(x => x.AddOrUpdate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<string, string>>()),
            Times.Once);
        Assert.Equal("someValue", actual);
    }

    [Fact]
    public void Get()
    {
        // Arrange
        var key = "someKey";
        var region = "someRegion";
        var value = "someValue";
        _manager
            .Setup(x => x.Get<string>(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(value);

        // Act
        var actual = _cacheManager.Get(key, region);

        // Assert
        Assert.Equal("someValue", actual);
    }

    [Fact]
    public void ClearRegion()
    {
        // Arrange
        _cacheManager.Add("fookey", "doesnt matter", "region", TTL);

        // Act
        _cacheManager.ClearRegion("region");

        // Assert
        _manager.Verify(x => x.ClearRegion("region"), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TryGetValue(bool isNull)
    {
        // Arrange
        CacheItem<string> item = isNull ? null : new CacheItem<string>("keyX", "valueX");
        _manager.Setup(x => x.GetCacheItem(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(item);

        // Act
        var actual = _cacheManager.TryGetValue("keyX", "someRegion", out var actualValue);

        Assert.Equal(!isNull, actual);
        Assert.Equal(!isNull ? "valueX" : null, actualValue);
    }
}
