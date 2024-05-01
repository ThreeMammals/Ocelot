using Ocelot.Configuration;
using Ocelot.RateLimiting;
using System.Reflection;

namespace Ocelot.UnitTests.RateLimiting;

public sealed class RateLimitingTests
{
    private readonly Mock<IRateLimitStorage> _storage;
    private readonly Ocelot.RateLimiting.RateLimiting _sut;

    public RateLimitingTests()
    {
        _storage = new();
        _sut = new(_storage.Object);
    }

    [Theory]
    [Trait("Feat", "37")]
    [InlineData(null)]
    [InlineData("")]
    public void ToTimespan_EmptyValue_ShouldReturnZero(string empty)
    {
        // Arrange, Act
        var actual = _sut.ToTimespan(empty);

        // Assert
        Assert.Equal(TimeSpan.Zero, actual);
    }

    [Theory]
    [Trait("Feat", "37")]
    [InlineData("1a")]
    [InlineData("2unknown")]
    public void ToTimespan_UnknownType_ShouldThrowFormatException(string timespan)
    {
        // Arrange, Act, Assert
        Assert.Throws<FormatException>(
            () => _sut.ToTimespan(timespan));
    }

    [Theory]
    [Trait("Feat", "37")]
    [InlineData("1s", 1 * TimeSpan.TicksPerSecond)]
    [InlineData("2m", 2 * TimeSpan.TicksPerMinute)]
    [InlineData("3h", 3 * TimeSpan.TicksPerHour)]
    [InlineData("4d", 4 * TimeSpan.TicksPerDay)]
    public void ToTimespan_KnownType_HappyPath(string timespan, long ticks)
    {
        // Arrange
        var expected = TimeSpan.FromTicks(ticks);

        // Act
        var actual = _sut.ToTimespan(timespan);

        // Assert
        Assert.Equal(expected, actual);
    }

    private static MethodInfo CountRequests() => typeof(Ocelot.RateLimiting.RateLimiting).GetMethod("CountRequests", BindingFlags.NonPublic | BindingFlags.Instance);

    [Fact]
    [Trait("Bug", "1590")]
    public void CountRequests_NoEntry_StartCounting()
    {
        // Arrange
        RateLimitCounter? arg1 = null; // No Entry
        RateLimitRule arg2 = null;

        // Act
        RateLimitCounter actual = (RateLimitCounter)CountRequests().Invoke(_sut, new object[] { arg1, arg2 });

        // Assert
        Assert.Equal(1L, actual.TotalRequests);
        Assert.True(DateTime.UtcNow - actual.Timestamp < TimeSpan.FromSeconds(1.0D));
    }

    [Fact]
    [Trait("Bug", "1590")]
    public void CountRequests_EntryHasNotExpired_IncrementedRequestCount()
    {
        // Arrange
        long total = 2;
        RateLimitCounter? arg1 = new RateLimitCounter(DateTime.UtcNow, total); // entry has not expired
        RateLimitRule arg2 = new("1s", 1.0D, 1L);

        // Act
        RateLimitCounter actual = (RateLimitCounter)CountRequests().Invoke(_sut, new object[] { arg1, arg2 });

        // Assert
        Assert.Equal(total + 1L, actual.TotalRequests); // incremented request count
        Assert.Equal(arg1.Value.Timestamp, actual.Timestamp); // starting point has not changed
    }

    [Fact]
    [Trait("Bug", "1590")]
    public void CountRequests_RateLimitExceeded_NoCounting()
    {
        // Arrange
        long total = 3, limit = total - 1;
        RateLimitCounter? arg1 = new RateLimitCounter(DateTime.UtcNow.AddSeconds(-2.0), total); // Entry has expired
        RateLimitRule arg2 = new("1s", 1.0D, limit); // rate limit exceeded

        // Act
        RateLimitCounter actual = (RateLimitCounter)CountRequests().Invoke(_sut, new object[] { arg1, arg2 });

        // Assert
        Assert.Equal(arg1.Value, actual); // No Counting, the counter was not changed
    }

    [Fact]
    [Trait("Bug", "1590")]
    public void CountRequests_RateLimitNotExceededAndPeriodIsElapsed_StartedCountingByDefault()
    {
        // Arrange
        long total = 3, limit = 3;
        RateLimitCounter? arg1 = new RateLimitCounter(DateTime.UtcNow.AddSeconds(-2.0), total); // Entry has expired
        RateLimitRule arg2 = new("1s", 1.0D, limit); // Rate limit not exceeded

        // Act
        RateLimitCounter actual = (RateLimitCounter)CountRequests().Invoke(_sut, new object[] { arg1, arg2 });

        // Assert
        Assert.Equal(1L, actual.TotalRequests); // started counting
        Assert.True(DateTime.UtcNow - actual.Timestamp < TimeSpan.FromSeconds(1.0D)); // started now
    }
}
