using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ocelot.RateLimiting;
using System;
using System.Text;
using Xunit;
using YamlDotNet.Core.Tokens;

namespace Ocelot.UnitTests.RateLimiting;

public class DistributedCacheRateLimitStorageTests
{
    private readonly Mock<IDistributedCache> _cache;
    private readonly DistributedCacheRateLimitStorage _storage;

    public DistributedCacheRateLimitStorageTests()
    {
        _cache = new Mock<IDistributedCache>();
        _storage = new DistributedCacheRateLimitStorage(_cache.Object);
    }

    [Fact]
    public void Set_ShouldSerializeAndStoreValue()
    {
        // Arrange
        var id = "test-id";
        var counter = new RateLimitCounter(DateTime.UtcNow, null, 5);
        var expiration = TimeSpan.FromMinutes(1);
        var expectedJson = JsonConvert.SerializeObject(counter);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedJson);
        _cache.Setup(c => c.Set(id, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>()));

        // Act
        _storage.Set(id, counter, expiration);

        // Assert
        _cache.Verify(c => c.Set(id, expectedBytes, It.Is<DistributedCacheEntryOptions>(opt => opt.AbsoluteExpirationRelativeToNow == expiration)),
            Times.Once);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("{\"StartedAt\":\"2025-09-11T12:00:00Z\",\"TotalRequests\":1}", true)]
    public void Exists_ShouldReturnCorrectBoolean(string storedValue, bool expected)
    {
        // Arrange
        var id = "exists-id";
        var bytes = Encoding.UTF8.GetBytes(storedValue);
        _cache.Setup(c => c.Get(id)).Returns(bytes);

        // Act
        var actual = _storage.Exists(id);

        // Assert
        _cache.Verify(c => c.Get(id), Times.Once);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Get_ShouldDeserializeStoredValue()
    {
        // Arrange
        var id = "get-id";
        var counter = new RateLimitCounter (DateTime.UtcNow, null, 3);
        var json = JsonConvert.SerializeObject(counter);
        var bytes = Encoding.UTF8.GetBytes(json);
        _cache.Setup(c => c.Get(id)).Returns(bytes);

        // Act
        var actual = _storage.Get(id);

        // Assert
        _cache.Verify(c => c.Get(id), Times.Once);
        Assert.NotNull(actual);
        Assert.Equal(counter.StartedAt, actual.Value.StartedAt);
        Assert.Equal(counter.TotalRequests, actual.Value.TotalRequests);
    }

    [Fact]
    public void Get_ShouldReturnNull_WhenStoredValueIsNullOrEmpty()
    {
        // Arrange
        var id = "null-id";
        var json = string.Empty;
        var bytes = Encoding.UTF8.GetBytes(json);
        _cache.Setup(c => c.Get(id)).Returns(bytes);

        // Act
        var actual = _storage.Get(id);

        // Assert
        _cache.Verify(c => c.Get(id), Times.Once);
        Assert.Null(actual);
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
