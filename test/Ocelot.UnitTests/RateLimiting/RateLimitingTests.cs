using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.RateLimiting;
using System.Runtime.CompilerServices;
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
    [Trait("PR", "1592")]
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
    [Trait("PR", "1592")]
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
    [Trait("PR", "1592")]
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
    [Trait("PR", "1592")]
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
    [Trait("PR", "1592")]
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
    [Trait("PR", "1592")]
    public void ProcessRequest_RateLimitExceededAndBanPeriodElapsed_StartedCounting()
    {
        // Arrange
        const double periodTimespan = 2.0D;
        const int millisecondsBeforeAfterEnding = 100; // current processing time of unit test should not take more 100 ms
        DateTime now = DateTime.UtcNow,
            startedAt = now.AddSeconds(-3).AddMilliseconds(millisecondsBeforeAfterEnding);
        DateTime? exceededAt = null;
        long totalRequests = 2L;
        TimeSpan expiration = TimeSpan.Zero;

        var (identity, options) = SetupProcessRequest("3s", periodTimespan, totalRequests,
            () => new RateLimitCounter(startedAt, exceededAt, totalRequests),
            (value) => expiration = value);

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
            Times.Never()); // Once()? Seems Remove is never called because of renewing
        _storage.Verify(x => x.Get(It.IsAny<string>()),
            Times.Exactly(2));
        _storage.Verify(x => x.Set(It.IsAny<string>(), It.IsAny<RateLimitCounter>(), It.IsAny<TimeSpan>()),
            Times.Exactly(2));
        Assert.Equal(TimeSpan.FromSeconds(3), expiration);
    }

    private (ClientRequestIdentity Identity, RateLimitOptions Options) SetupProcessRequest(string period, double periodTimespan, long limit,
        Func<RateLimitCounter?> counterFactory, Action<TimeSpan> expirationAction, [CallerMemberName] string testName = "")
    {
        ClientRequestIdentity identity = new(nameof(RateLimitingTests), "/" + testName, HttpMethods.Get);
        RateLimitOptions options = new RateLimitOptionsBuilder()
            .WithEnableRateLimiting(true)
            .WithRateLimitCounterPrefix(nameof(_RateLimiting_.ProcessRequest))
            .WithRateLimitRule(new RateLimitRule(period, periodTimespan, limit))
            .Build();
        _storage.Setup(x => x.Get(It.IsAny<string>()))
            .Returns(counterFactory); // counter value factory
        _storage.Setup(x => x.Remove(It.IsAny<string>()))
            .Verifiable();
        expirationAction?.Invoke(TimeSpan.Zero);
        _storage.Setup(x => x.Set(It.IsAny<string>(), It.IsAny<RateLimitCounter>(), It.IsAny<TimeSpan>()))
            .Callback<string, RateLimitCounter, TimeSpan>((id, counter, expirationTime) => expirationAction?.Invoke(expirationTime))
            .Verifiable();
        return (identity, options);
    }

    [Fact]
    [Trait("Bug", "1590")]
    public void ProcessRequest_PeriodTimespanValueIsGreaterThanPeriod_ExpectedBehaviorAndExpirationInPeriod()
    {
        // Arrange: user scenario
        const string period = "1s";
        const double periodTimespan = 30.0D; // seconds
        const long limit = 100L, requestsPerSecond = 20L;

        // Arrange: setup
        DateTime? startedAt = null;
        TimeSpan expiration = TimeSpan.Zero;
        long total = 1L, count = requestsPerSecond;
        RateLimitCounter? current = null;
        var (identity, options) = SetupProcessRequest(period, periodTimespan, limit,
            () => current,
            (value) => expiration = value);

        // Arrange 20 requests per period (1 sec)
        var periodSeconds = TimeSpan.FromSeconds(double.Parse(period[0].ToString()));
        var periodMilliseconds = periodSeconds.TotalMilliseconds;
        int delay = (int)((periodMilliseconds - 200) / requestsPerSecond); // 20 requests per 1 second

        while (count > 0L)
        {
            // Act
            var actual = _sut.ProcessRequest(identity, options);

            // life hack for the 1st request
            if (count == requestsPerSecond)
            {
                startedAt = actual.StartedAt; // for the 1st request get expected value
            }

            // Assert
            Assert.True(actual.TotalRequests < limit);
            actual.TotalRequests.ShouldBe(total++, $"Count is {count}");
            Assert.Equal(startedAt, actual.StartedAt); // starting point is not changed
            Assert.Null(actual.ExceededAt); // no exceeding at all
            Assert.Equal(periodSeconds, expiration); // expiration in the period

            // Arrange: next micro test
            current = actual;
            Thread.Sleep(delay);
            count--;
        }
        
        Assert.NotEqual(TimeSpan.FromSeconds(periodTimespan), expiration); // Not ban period expiration
        Assert.Equal(periodSeconds, expiration); // last 20th request was in counting period
    }
}
