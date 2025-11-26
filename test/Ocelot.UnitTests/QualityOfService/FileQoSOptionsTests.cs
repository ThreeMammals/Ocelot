using Ocelot.Configuration;
using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.QualityOfService;

public class FileQoSOptionsTests
{
    [Fact]
    [Trait("PR", "2073")]
    [Trait("PR", "2081")]
    public void Ctor_Default_AllPropertiesAreNull()
    {
        // Arrange, Act
        var actual = new FileQoSOptions();

        // Assert
        Assert.Null(actual.DurationOfBreak);
        Assert.Null(actual.BreakDuration);
        Assert.Null(actual.ExceptionsAllowedBeforeBreaking);
        Assert.Null(actual.MinimumThroughput);
        Assert.Null(actual.FailureRatio);
        Assert.Null(actual.SamplingDuration);
        Assert.Null(actual.TimeoutValue);
        Assert.Null(actual.Timeout);
    }

    [Fact]
    [Trait("PR", "2081")]
    [Trait("Feat", "2080")]
    public void Ctor_Copying_Copied()
    {
        // Arrange
        FileQoSOptions expected = new()
        {
            DurationOfBreak = 1,
            BreakDuration = 2,
            ExceptionsAllowedBeforeBreaking = 3,
            MinimumThroughput = 4,
            FailureRatio = 5.0D,
            SamplingDuration = 6,
            TimeoutValue = 7,
            Timeout = 8,
        };

        // Act
        FileQoSOptions actual = new(expected); // copying

        // Assert
        Assert.Equivalent(expected, actual);
        AssertEquality(actual, expected);
    }

    [Fact]
    [Trait("PR", "2081")]
    [Trait("Feat", "2080")]
    public void Ctor_CopyingQoSOptions_Copied()
    {
        // Arrange
        FileQoSOptions expected = new()
        {
            DurationOfBreak = 3,
            BreakDuration = 3,
            ExceptionsAllowedBeforeBreaking = 2,
            MinimumThroughput = 2,
            FailureRatio = 4.0D,
            SamplingDuration = 5,
            TimeoutValue = 6,
            Timeout = 6,
        };
        QoSOptions from = new(2, 3)
        {
            FailureRatio = 4.0D,
            SamplingDuration = 5,
            Timeout = 6,
        };

        // Act
        FileQoSOptions actual = new(from); // copying

        // Assert
        Assert.Equivalent(expected, actual);
        AssertEquality(actual, expected);
    }

    private static void AssertEquality(FileQoSOptions actual, FileQoSOptions expected)
    {
        Assert.Equal(expected.DurationOfBreak, actual.DurationOfBreak);
        Assert.Equal(expected.BreakDuration, actual.BreakDuration);
        Assert.Equal(expected.ExceptionsAllowedBeforeBreaking, actual.ExceptionsAllowedBeforeBreaking);
        Assert.Equal(expected.MinimumThroughput, actual.MinimumThroughput);
        Assert.Equal(expected.FailureRatio, actual.FailureRatio);
        Assert.Equal(expected.SamplingDuration, actual.SamplingDuration);
        Assert.Equal(expected.TimeoutValue, actual.TimeoutValue);
        Assert.Equal(expected.Timeout, actual.Timeout);
    }
}
