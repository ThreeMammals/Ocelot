using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.RateLimiting;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using _RateLimiting_ = Ocelot.RateLimiting.RateLimiting;

namespace Ocelot.UnitTests.RateLimiting;

public class RateLimitingTests : RateLimitingTestsBase
{
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
        DateTime now = DateTime.UtcNow;
        RateLimitCounter? arg1 = null; // No Entry
        RateLimitRule arg2 = null;

        // Act
        RateLimitCounter actual = _sut.Count(arg1, arg2, now);

        // Assert
        Assert.Equal(1L, actual.Total);
        Assert.True(now - actual.StartedAt < TimeSpan.FromSeconds(1.0D));
    }

    [Fact]
    [Trait("PR", "1592")]
    public void Count_EntryHasNotExpired_IncrementedRequestCount()
    {
        // Arrange
        long total = 2;
        DateTime now = DateTime.UtcNow;
        RateLimitCounter? arg1 = new RateLimitCounter(now, null, total); // entry has not expired
        RateLimitRule arg2 = new("1s", "1s", total + 1); // with not exceeding limit

        // Act
        RateLimitCounter actual = _sut.Count(arg1, arg2, now);

        // Assert
        Assert.Equal(total + 1, actual.Total); // incremented request count
        Assert.Equal(arg1.Value.StartedAt, actual.StartedAt); // starting point has not changed
    }

    [Fact]
    [Trait("PR", "1592")]
    public void Count_EntryHasNotExpiredAndExceedingLimit_IncrementedRequestCountWithRenewedStartMoment()
    {
        // Arrange
        long total = 2;
        DateTime now = DateTime.UtcNow;
        RateLimitCounter? arg1 = new RateLimitCounter(now, null, total); // entry has not expired
        RateLimitRule arg2 = new("1s", "1s", 1L);

        // Act
        RateLimitCounter actual = _sut.Count(arg1, arg2, now);

        // Assert
        Assert.Equal(total + 1, actual.Total); // incremented request count
        Assert.InRange(actual.StartedAt, arg1.Value.StartedAt, now); // starting point has renewed and it is between StartedAt and Now
    }

    [Fact]
    [Trait("PR", "1592")]
    public void Count_RateLimitExceeded_StartedCounting()
    {
        // Arrange
        long total = 3, limit = total - 1;
        DateTime now = DateTime.UtcNow;
        RateLimitRule rule = new("1s", "2s", limit); // rate limit exceeded
        var entry = new RateLimitCounter(
            now.AddSeconds(-rule.PeriodSpan.TotalSeconds - rule.WaitSpan.TotalSeconds),
            now.AddSeconds(-rule.WaitSpan.TotalSeconds),
            total); // Entry has expired

        // Act
        var futureIsNow = now.AddMilliseconds(1); // let's move to the future to allow the waiting period to pass
        RateLimitCounter actual = _sut.Count(entry, rule, futureIsNow);

        // Assert
        Assert.Equal(1L, actual.Total); // started counting, the counter was changed
        Assert.Equal(futureIsNow, actual.StartedAt); // started now
    }

    [Fact]
    [Trait("PR", "1592")]
    public void Count_PeriodIsElapsedAndWaitPeriodIsElapsed_StartedNewCountingPeriod()
    {
        // Arrange
        long total = 3, limit = 3;
        DateTime now = DateTime.UtcNow;
        RateLimitRule rule = new("1s", "1s", limit);
        RateLimitCounter? entry = new(
            now.AddSeconds(-rule.PeriodSpan.TotalSeconds - rule.WaitSpan.TotalSeconds), // 2 seconds ago
            now.AddSeconds(-rule.WaitSpan.TotalSeconds), // 1 second ago
            total); // Entry is about to expire

        // Act, Assert 1
        RateLimitCounter actual = _sut.Count(entry, rule, now); // at the moment of wait period elapsing, inclusively
        Assert.Equal(4, actual.Total); // started counting
        Assert.True(actual.ExceededAt.HasValue); // old counter is valid

        // Act, Assert 2
        var futureIsNow = now.AddMilliseconds(1); // let's move to the future to allow the waiting period to pass
        actual = _sut.Count(entry, rule, futureIsNow);
        Assert.Equal(1L, actual.Total); // started counting
        Assert.Equal(futureIsNow, actual.StartedAt); // started now
    }

    [Fact]
    [Trait("PR", "1592")]
    public void ProcessRequest_QuotaExceededAndWaitPeriodElapsed_StartedCountingViaResettingCounter()
    {
        // Arrange
        const string fixedWindow = "3s", waitWindow = "2s";
        RateLimitRule rule = new(fixedWindow, waitWindow, 2);
        DateTime now = DateTime.UtcNow, startedAt = now.AddSeconds(-rule.PeriodSpan.TotalSeconds);
        DateTime? exceededAt = null;
        long totalRequests = 2L;
        TimeSpan expiration = TimeSpan.Zero;

        var (identity, options) = SetupProcessRequest(fixedWindow, waitWindow, totalRequests,
            () => new RateLimitCounter(startedAt, exceededAt, totalRequests),
            (value) => expiration = value);

        // Act 1
        var counter = _sut.ProcessRequest(identity, options, now);

        // Assert 1
        Assert.Equal(3L, counter.Total); // old counting -> 3
        Assert.Equal(startedAt, counter.StartedAt); // starting point was not changed
        Assert.True(counter.ExceededAt.HasValue); // exceeded
        Assert.Equal(now, counter.ExceededAt.Value); // exceeded now, in the same second

        // Arrange 2
        startedAt = counter.StartedAt; // move to past
        exceededAt = counter.ExceededAt; // move to past
        totalRequests = counter.Total; // 3
        now += rule.WaitSpan; // don't wait, just move to future

        // Act 2
        var actual = _sut.ProcessRequest(identity, options, now);

        // Assert
        Assert.Equal(1L, actual.Total); // started counting
        Assert.Equal(actual.StartedAt, now); // starting point has renewed and it is Now
        Assert.Null(actual.ExceededAt);
        _storage.Verify(x => x.Remove(It.IsAny<string>()),
            Times.Never()); // Once()? Seems Remove is never called because of renewing
        _storage.Verify(x => x.Get(It.IsAny<string>()),
            Times.Exactly(2));
        _storage.Verify(x => x.Set(It.IsAny<string>(), It.IsAny<RateLimitCounter>(), It.IsAny<TimeSpan>()),
            Times.Exactly(2));
        Assert.Equal(6, expiration.TotalSeconds);
    }

    [Fact]
    [Trait("PR", "2294")]
    public void RetryAfter_NoQuotaExceeding_NoNeedToRetry()
    {
        // Arrange
        long total = 2, limit = 3;
        DateTime now = DateTime.UtcNow;
        RateLimitRule rule = new("1s", "1s", limit);
        RateLimitCounter counter = new(
            now.AddSeconds(-rule.PeriodSpan.TotalSeconds / 2),
            null, total);

        // Act
        double actual = _sut.RetryAfter(counter, rule, now);

        // Assert
        Assert.Equal(0.0, actual);
    }

    [Theory]
    [Trait("PR", "2294")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(RateLimitRule.ZeroWait)]
    public void RetryAfter_DoNotWait_RetryAfterTheHalfOfPeriod(string doNotWait)
    {
        // Arrange
        long total = 4, limit = 3;
        DateTime now = DateTime.UtcNow;
        RateLimitRule rule = new("1s", doNotWait, limit);
        RateLimitCounter counter = new(
            startedAt: now.AddSeconds(-rule.PeriodSpan.TotalSeconds / 2),
            exceededAt: now,
            totalRequests: total);

        // Act
        double actual = _sut.RetryAfter(counter, rule, now);

        // Assert
        Assert.Equal(0.5, actual);
    }

    [Fact]
    [Trait("PR", "2294")]
    public void RetryAfter_ExceedingInWaitingWindow_RetryAfterTheQuarterOfWaitPeriod()
    {
        // Arrange
        long total = 4, limit = 3;
        DateTime now = DateTime.UtcNow;
        RateLimitRule rule = new("1s", "1s", limit);
        RateLimitCounter counter = new(
            startedAt: now.AddSeconds(-(rule.PeriodSpan.TotalSeconds / 2) - (rule.WaitSpan.TotalSeconds / 4 * 3)),
            exceededAt: now.AddSeconds(-(rule.WaitSpan.TotalSeconds / 4 * 3)),
            totalRequests: total);

        // Act
        double actual = _sut.RetryAfter(counter, rule, now);

        // Assert
        Assert.Equal(0.25, actual);
    }

    [Fact]
    [Trait("PR", "2294")]
    public void RetryAfter_Exceeding_WaitingPeriodElapsed_NoNeedToRetry()
    {
        // Arrange
        long total = 4, limit = 3;
        DateTime now = DateTime.UtcNow;
        RateLimitRule rule = new("1s", "1s", limit);
        RateLimitCounter counter = new(
            startedAt: now.AddSeconds(-rule.PeriodSpan.TotalSeconds - rule.WaitSpan.TotalSeconds),
            exceededAt: now.AddSeconds(-rule.WaitSpan.TotalSeconds),
            totalRequests: total);

        // Act
        double actual = _sut.RetryAfter(counter, rule, now);

        // Assert
        Assert.Equal(-1.0, actual);
    }

    [Collection(nameof(SequentialTests))]
    public class Sequential : RateLimitingTestsBase
    {
        [SkippableFact]
        [Trait("Bug", "1590")]
        public void ProcessRequest_PeriodTimespanValueIsGreaterThanPeriod_ExpectedBehaviorAndExpirationInPeriod()
        {
            // The test is stable in Linux and Windows only
            Skip.If(RuntimeInformation.IsOSPlatform(OSPlatform.OSX), "Skip in MacOS because the test is very unstable");

            // Arrange: user scenario
            const long limit = 100L, requestsPerSecond = 20L;
            const string fixedWindow = "1s", waitWindow = "30s";
            RateLimitRule rule = new(fixedWindow, waitWindow, 2);

            // Arrange: setup
            DateTime now = DateTime.UtcNow;
            DateTime? startedAt = null;
            TimeSpan expiration = TimeSpan.Zero;
            long total = 1L, count = requestsPerSecond;
            RateLimitCounter? current = null;
            
            var (identity, options) = SetupProcessRequest(fixedWindow, waitWindow, limit,
                () => current,
                (value) => expiration = value);

            // Arrange 20 requests per period (1 sec)
            var periodMilliseconds = rule.PeriodSpan.TotalMilliseconds;
            int delay = (int)((periodMilliseconds - 200) / requestsPerSecond); // 20 requests per 1 second

            while (count > 0L)
            {
                // Act
                var actual = _sut.ProcessRequest(identity, options, now);

                // life hack for the 1st request
                if (count == requestsPerSecond)
                {
                    startedAt = actual.StartedAt; // for the 1st request get expected value
                }

                // Assert
                Assert.True(actual.Total < limit);
                actual.Total.ShouldBe(total++, $"Count is {count}");
                Assert.Equal(startedAt, actual.StartedAt); // starting point is not changed
                Assert.Null(actual.ExceededAt); // no exceeding at all
                Assert.Equal(32, expiration.TotalSeconds);

                // Arrange: next micro test
                current = actual;
                Thread.Sleep(delay);
                count--;
            }

            Assert.NotEqual(rule.WaitSpan, expiration); // Not Wait period expiration
            Assert.Equal(32, expiration.TotalSeconds); // last 20th request was in counting period
        }
    }
}

public class RateLimitingTestsBase
{
    protected readonly Mock<IRateLimitStorage> _storage;
    protected readonly _RateLimiting_ _sut;
    public RateLimitingTestsBase()
    {
        _storage = new();
        _sut = new(_storage.Object);
    }

    protected (ClientRequestIdentity Identity, RateLimitOptions Options) SetupProcessRequest(string fixedWindow, string waitWindow, long limit,
        Func<RateLimitCounter?> counterFactory, Action<TimeSpan> expirationAction, [CallerMemberName] string testName = "")
    {
        ClientRequestIdentity identity = new(nameof(RateLimitingTests) + "/" + testName, HttpMethods.Get);
        RateLimitOptions options = new()
        {
            EnableRateLimiting = true,
            KeyPrefix = nameof(_RateLimiting_.ProcessRequest),
            Rule = new(fixedWindow, waitWindow, limit),
        };
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
}
