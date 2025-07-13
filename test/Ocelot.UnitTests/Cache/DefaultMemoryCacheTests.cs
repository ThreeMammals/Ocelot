using Microsoft.Extensions.Caching.Memory;
using Ocelot.Cache;

namespace Ocelot.UnitTests.Cache;

public class DefaultMemoryCacheTests : UnitTest
{
    private readonly DefaultMemoryCache<Fake> _cache;

    public DefaultMemoryCacheTests()
    {
        _cache = new DefaultMemoryCache<Fake>(new MemoryCache(new MemoryCacheOptions()));
    }

    [Fact]
    public void Should_cache()
    {
        // Arrange
        var fake = new Fake(1);
        _cache.Add("1", fake, TimeSpan.FromSeconds(100), "region");

        // Act
        var result = _cache.Get("1", "region");

        // Assert
        result.ShouldBe(fake);
        fake.Value.ShouldBe(1);
    }

    [Fact]
    public void Doesnt_exist()
    {
        // Arrange, Act
        var result = _cache.Get("1", "region");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Should_add_and_delete()
    {
        // Arrange
        var fake = new Fake(1);
        _cache.Add("1", fake, TimeSpan.FromSeconds(100), "region");
        var newFake = new Fake(1);
        _cache.AddAndDelete("1", newFake, TimeSpan.FromSeconds(100), "region");

        // Act
        var result = _cache.Get("1", "region");

        // Assert
        result.ShouldBe(newFake);
        newFake.Value.ShouldBe(1);
    }

    [Fact]
    public void Should_clear_region()
    {
        // Arrange
        var fake1 = new Fake(1);
        var fake2 = new Fake(2);
        _cache.Add("1", fake1, TimeSpan.FromSeconds(100), "region");
        _cache.Add("2", fake2, TimeSpan.FromSeconds(100), "region");
        _cache.ClearRegion("region");

        // Act, Assert
        var result1 = _cache.Get("1", "region");
        result1.ShouldBeNull();

        // Act, Assert
        var result2 = _cache.Get("2", "region");
        result2.ShouldBeNull();
    }

    [Fact]
    public void Should_clear_key_if_ttl_expired()
    {
        // Arrange
        var fake = new Fake(1);
        _cache.Add("1", fake, TimeSpan.FromMilliseconds(50), "region");
        Thread.Sleep(200);

        // Act
        var result = _cache.Get("1", "region");

        // Assert
        result.ShouldBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Should_not_add_to_cache_if_timespan_empty(int ttl)
    {
        // Arrange
        var fake = new Fake(1);
        _cache.Add("1", fake, TimeSpan.FromSeconds(ttl), "region");

        // Act
        var result = _cache.Get("1", "region");

        // Assert
        result.ShouldBeNull();
    }

    private class Fake
    {
        public Fake(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }
}
