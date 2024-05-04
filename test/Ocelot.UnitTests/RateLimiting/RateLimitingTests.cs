using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.RateLimiting;
using _RateLimiting_ = Ocelot.RateLimiting.RateLimiting;

namespace Ocelot.UnitTests.RateLimiting;

public sealed class RateLimitingTests
{
    private readonly Mock<IRateLimitStorage> _storage;
    private readonly _RateLimiting_ _sut;

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

    [Fact]
    [Trait("Bug", "1590")]
    public void Count_NoEntry_StartCounting()
    {
        // Arrange
        RateLimitCounter? arg1 = null; // No Entry
        RateLimitRule arg2 = null;

        // Act
        RateLimitCounter actual = _sut.Count(arg1, arg2);

        // Assert
        Assert.Equal(1L, actual.TotalRequests);
        Assert.True(DateTime.UtcNow - actual.StartedAt < TimeSpan.FromSeconds(1.0D));
    }

    [Fact]
    [Trait("Bug", "1590")]
    public void Count_EntryHasNotExpired_IncrementedRequestCount()
    {
        // Arrange
        long total = 2;
        RateLimitCounter? arg1 = new RateLimitCounter(DateTime.UtcNow, null, total); // entry has not expired
        RateLimitRule arg2 = new("1s", 1.0D, total + 1); // with not exceeding limit

        // Act
        RateLimitCounter actual = _sut.Count(arg1, arg2);

        // Assert
        Assert.Equal(total + 1, actual.TotalRequests); // incremented request count
        Assert.Equal(arg1.Value.StartedAt, actual.StartedAt); // starting point has not changed
    }

    [Fact]
    [Trait("Bug", "1590")]
    public void Count_EntryHasNotExpiredAndExceedingLimit_IncrementedRequestCountWithRenewedStartMoment()
    {
        // Arrange
        long total = 2;
        RateLimitCounter? arg1 = new RateLimitCounter(DateTime.UtcNow, null, total); // entry has not expired
        RateLimitRule arg2 = new("1s", 1.0D, 1L);

        // Act
        RateLimitCounter actual = _sut.Count(arg1, arg2);

        // Assert
        Assert.Equal(total + 1, actual.TotalRequests); // incremented request count
        Assert.InRange(actual.StartedAt, arg1.Value.StartedAt, DateTime.UtcNow); // starting point has renewed and it is between StartedAt and Now
    }

    [Fact]
    [Trait("Bug", "1590")]
    public void Count_RateLimitExceeded_StartedCounting()
    {
        // Arrange
        long total = 3, limit = total - 1;
        TimeSpan periodTimespan = TimeSpan.FromSeconds(1.0D);
        DateTime startedAt = DateTime.UtcNow.AddSeconds(-2.0), // 2 secs ago
            exceededAt = startedAt + periodTimespan; // 1 second ago
        RateLimitCounter? arg1 = new RateLimitCounter(startedAt, exceededAt, total); // Entry has expired
        RateLimitRule arg2 = new("1s", periodTimespan.TotalSeconds, limit); // rate limit exceeded

        // Act
        RateLimitCounter actual = _sut.Count(arg1, arg2);

        // Assert
        Assert.Equal(1L, actual.TotalRequests); // started counting, the counter was changed
        Assert.InRange(actual.StartedAt, arg1.Value.ExceededAt.Value, DateTime.UtcNow); // starting point has renewed and it is between exceededAt and Now
    }

    [Fact]
    [Trait("Bug", "1590")]
    public void Count_RateLimitNotExceededAndPeriodIsElapsed_StartedCountingByDefault()
    {
        // Arrange
        long total = 3, limit = 3;
        RateLimitCounter? arg1 = new RateLimitCounter(DateTime.UtcNow.AddSeconds(-2.0), null, total); // Entry has expired
        RateLimitRule arg2 = new("1s", 1.0D, limit); // Rate limit not exceeded

        // Act
        RateLimitCounter actual = _sut.Count(arg1, arg2);

        // Assert
        Assert.Equal(1L, actual.TotalRequests); // started counting
        Assert.True(DateTime.UtcNow - actual.StartedAt < TimeSpan.FromSeconds(1.0D)); // started now
    }

    [Fact]
    [Trait("Bug", "1590")]
    public void ProcessRequest_RateLimitExceededAndBanPeriodElapsed_StartedCounting()
    {
        // Arrange
        ClientRequestIdentity identity = new(
            nameof(RateLimitingTests),
            "/" + nameof(ProcessRequest_RateLimitExceededAndBanPeriodElapsed_StartedCounting),
            HttpMethods.Get);
        const double periodTimespan = 2.0D;
        RateLimitOptions options = new RateLimitOptionsBuilder()
            .WithEnableRateLimiting(true)
            .WithRateLimitCounterPrefix(nameof(_RateLimiting_.ProcessRequest))
            .WithRateLimitRule(new("3s", periodTimespan, 2L))
            .Build();
        const int millisecondsBeforeAfterEnding = 100; // current processing time of unit test should not take more 100 ms
        DateTime now = DateTime.UtcNow,
            startedAt = now.AddSeconds(-3).AddMilliseconds(millisecondsBeforeAfterEnding);
        DateTime? exceededAt = null;
        long totalRequests = 2L;
        _storage.Setup(x => x.Get(It.IsAny<string>()))
            .Returns(() => new RateLimitCounter(startedAt, exceededAt, totalRequests)); // counter value factory
        _storage.Setup(x => x.Remove(It.IsAny<string>()))
            .Verifiable();
        TimeSpan expiration = TimeSpan.Zero;
        _storage.Setup(x => x.Set(It.IsAny<string>(), It.IsAny<RateLimitCounter>(), It.IsAny<TimeSpan>()))
            .Callback<string, RateLimitCounter, TimeSpan>((id, counter, expirationTime) => expiration = expirationTime)
            .Verifiable();

        // Act 1
        var counter = _sut.ProcessRequest(identity, options);

        // Assert 1
        Assert.Equal(3L, counter.TotalRequests); // old counting -> 3
        Assert.Equal(startedAt, counter.StartedAt); // starting point was not changed
        Assert.NotNull(counter.ExceededAt); // exceeded
        Assert.Equal(DateTime.UtcNow.Second, counter.ExceededAt.Value.Second); // exceeded now, in the same second

        // Arrange 2
        TimeSpan shift = TimeSpan.FromSeconds(periodTimespan); // don't wait, just move to future
        startedAt = counter.StartedAt - shift; // move to past
        exceededAt = counter.ExceededAt - shift; // move to past
        totalRequests = counter.TotalRequests; // 3

        // Act 2
        var actual = _sut.ProcessRequest(identity, options);

        // Assert
        Assert.Equal(1L, actual.TotalRequests); // started counting
        Assert.InRange(actual.StartedAt, now, DateTime.UtcNow); // starting point has renewed and it is between test starting and Now
        Assert.Null(actual.ExceededAt);
        _storage.Verify(x => x.Remove(It.IsAny<string>()),
            Times.Never()); // Times.Once()? Seems Remove is never called because of renewing
        _storage.Verify(x => x.Get(It.IsAny<string>()),
            Times.Exactly(2));
        _storage.Verify(x => x.Set(It.IsAny<string>(), It.IsAny<RateLimitCounter>(), It.IsAny<TimeSpan>()),
            Times.Exactly(2));
        Assert.Equal(TimeSpan.FromSeconds(3), expiration);
    }
}
