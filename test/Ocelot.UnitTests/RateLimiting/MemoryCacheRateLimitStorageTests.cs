using Microsoft.Extensions.Caching.Memory;
using Ocelot.RateLimiting;

namespace Ocelot.UnitTests.RateLimiting;

public class MemoryCacheRateLimitStorageTests
{
    private readonly Mock<IMemoryCache> _cache;
    private readonly Mock<ICacheEntry> _entry;
    private readonly MemoryCacheRateLimitStorage _storage;

    public MemoryCacheRateLimitStorageTests()
    {
        _cache = new Mock<IMemoryCache>();
        _entry = new Mock<ICacheEntry>();
        _storage = new MemoryCacheRateLimitStorage(_cache.Object);
    }

    [Fact]
    public void Set_ShouldCreateEntryAndSetValue()
    {
        // Arrange
        var id = "test-id";
        var counter = new RateLimitCounter(DateTime.UtcNow, null, 1);
        TimeSpan expiration = TimeSpan.FromMinutes(5);
        _cache.Setup(c => c.CreateEntry(id)).Returns(_entry.Object);

        // Act
        _storage.Set(id, counter, expiration);

        // Assert
        _entry.VerifySet(e => e.Value = counter);
        _entry.VerifySet(e => e.AbsoluteExpirationRelativeToNow = expiration);
        _entry.Verify(e => e.Dispose());
        _cache.Verify(c => c.CreateEntry(id), Times.Once);
    }

    [Fact]
    public void Exists_ShouldReturnTrue_WhenKeyExists()
    {
        // Arrange
        var id = "exists-id";
        var counter = new RateLimitCounter(DateTime.UtcNow, null, 2);
        object boxed = counter;
        _cache.Setup(c => c.TryGetValue(id, out boxed))
            .Returns(true);

        // Act
        var actual = _storage.Exists(id);

        // Assert
        Assert.True(actual);
        _cache.Verify(c => c.TryGetValue(id, out boxed), Times.Once);
    }

    [Fact]
    public void Exists_ShouldReturnFalse_WhenKeyDoesNotExist()
    {
        // Arrange
        var id = "missing-id";
        object boxed = null;
        _cache.Setup(c => c.TryGetValue(id, out boxed))
            .Returns(false);

        // Act
        var actual = _storage.Exists(id);

        // Assert
        Assert.False(actual);
        _cache.Verify(c => c.TryGetValue(id, out boxed), Times.Once);
    }

    [Fact]
    public void Get_ShouldReturnCounter_WhenKeyExists()
    {
        // Arrange
        var id = "get-id";
        var counter = new RateLimitCounter(DateTime.UtcNow, null, 3);
        object boxed = counter;
        _cache.Setup(c => c.TryGetValue(id, out boxed))
            .Returns(true);

        // Act
        var result = _storage.Get(id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(counter.TotalRequests, result.Value.TotalRequests);
        Assert.Equal(counter.StartedAt, result.Value.StartedAt);
        _cache.Verify(c => c.TryGetValue(id, out boxed), Times.Once);
    }

    [Fact]
    public void Get_ShouldReturnNull_WhenKeyDoesNotExist()
    {
        // Arrange
        var id = "null-id";
        object boxed = null;
        _cache.Setup(c => c.TryGetValue(id, out boxed))
            .Returns(false);

        // Act
        var actual = _storage.Get(id);

        // Assert
        Assert.Null(actual);
        _cache.Verify(c => c.TryGetValue(id, out boxed), Times.Once);
    }

    [Fact]
    public void Remove_ShouldCallCacheRemove()
    {
        // Arrange
        var id = "remove-id";

        // Act
        _storage.Remove(id);

        // Assert
        _cache.Verify(c => c.Remove(id), Times.Once);
    }
}
