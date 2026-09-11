using Ocelot.Configuration;
using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.QualityOfService;

[Trait("Feat", "585")]
[Trait("Feat", "2338")] // https://github.com/ThreeMammals/Ocelot/issues/2338
[Trait("PR", "2339")] // https://github.com/ThreeMammals/Ocelot/pull/2339
public class FileGlobalQoSOptionsTests
{
    [Fact]
    public void Ctor()
    {
        // Arrange
        var actual = new FileGlobalQoSOptions();

        // Assert
        AssertNullProps(actual);
    }

    [Fact]
    public void Ctor_FileQoSOptions()
    {
        // Arrange
        FileQoSOptions from = new()
        {
            DurationOfBreak = 1,
            BreakDuration = 2,
            ExceptionsAllowedBeforeBreaking = 3,
            MinimumThroughput = 4,
            FailureRatio = 5,
            SamplingDuration = 6,
            TimeoutValue = 7,
            Timeout = 8,
        };

        // Act
        FileGlobalQoSOptions actual = new(from);

        // Assert
        Assert.NotSame(from, actual);
        Assert.Equivalent(from, actual);
        Assert.Null(actual.RouteKeys);
    }

    [Fact]
    public void Ctor_QoSOptions()
    {
        // Arrange
        QoSOptions from = new(2, 3)
        {
            FailureRatio = 4.0D,
            SamplingDuration = 5,
            Timeout = 6,
        };

        // Act
        FileGlobalQoSOptions actual = new(from);

        // Assert
        Assert.NotSame(from, actual);
        Assert.Null(actual.RouteKeys);
        Assert.Equal(from.BreakDuration, actual.DurationOfBreak);
        Assert.Equal(from.BreakDuration, actual.BreakDuration);
        Assert.Equal(from.MinimumThroughput, actual.ExceptionsAllowedBeforeBreaking);
        Assert.Equal(from.MinimumThroughput, actual.MinimumThroughput);
        Assert.Equal(from.FailureRatio, actual.FailureRatio);
        Assert.Equal(from.SamplingDuration, actual.SamplingDuration);
        Assert.Equal(from.Timeout, actual.TimeoutValue);
        Assert.Equal(from.Timeout, actual.Timeout);
    }

    private static void AssertNullProps(FileGlobalQoSOptions actual)
    {
        Assert.NotNull(actual);
        Assert.Null(actual.RouteKeys);

        Assert.Null(actual.DurationOfBreak);
        Assert.Null(actual.BreakDuration);
        Assert.Null(actual.ExceptionsAllowedBeforeBreaking);
        Assert.Null(actual.MinimumThroughput);
        Assert.Null(actual.FailureRatio);
        Assert.Null(actual.SamplingDuration);
        Assert.Null(actual.TimeoutValue);
        Assert.Null(actual.Timeout);
    }
}
