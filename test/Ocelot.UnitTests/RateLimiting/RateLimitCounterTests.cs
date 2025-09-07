using Ocelot.RateLimiting;

namespace Ocelot.UnitTests.RateLimiting;

public class RateLimitCounterTests
{
    [Fact]
    public void Ctor()
    {
        // Arrange
        var startedAt = DateTime.Now;
        var exceededAt = startedAt + TimeSpan.FromSeconds(1);

        // Act
        var actual = new RateLimitCounter(startedAt, exceededAt, 3);

        // Assert
        Assert.Equal(startedAt, actual.StartedAt);
        Assert.Equal(exceededAt, actual.ExceededAt);
        Assert.Equal(3, actual.TotalRequests);
    }

    [Fact]
    [Trait("Feat", "1229")] // https://github.com/ThreeMammals/Ocelot/issues/1229
    [Trait("PR", "2294")] // https://github.com/ThreeMammals/Ocelot/pull/2294
    public void ToString_NoExceededAt()
    {
        // Arrange
        var today = new DateTime(2025, 9, 7);
        today = today.AddHours(12);
        today = today.AddMinutes(34);
        today = today.AddSeconds(56.789);
        today = today.AddMicroseconds(123.4567890);
        RateLimitCounter counter = new(today, default, 1);

        // Act
        var actual = counter.ToString();

        // Assert
        Assert.Equal("1->(2025-09-07T12:34:56.7891234)", actual);
    }

    [Fact]
    [Trait("Feat", "1229")] // https://github.com/ThreeMammals/Ocelot/issues/1229
    [Trait("PR", "2294")] // https://github.com/ThreeMammals/Ocelot/pull/2294
    public void ToString_WithExceededAt()
    {
        // Arrange
        var today = new DateTime(2025, 9, 7);
        today = today.AddHours(1);
        today = today.AddMinutes(2);
        today = today.AddSeconds(3);
        today = today.AddMilliseconds(4);
        today = today.AddMicroseconds(5);
        TimeSpan shift = new(1, 2, 3, 4, 5, 6);
        DateTime? exceededAt = today + shift;
        RateLimitCounter counter = new(today, exceededAt, 2);

        // Act
        var actual = counter.ToString();

        // Assert
        Assert.Equal("2->(2025-09-07T01:02:03.0040050)+1.02:03:04.0050060", actual);
    }
}
