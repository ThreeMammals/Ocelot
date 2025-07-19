using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Configuration.FileModels;

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
        Assert.Null(actual.ExceptionsAllowedBeforeBreaking);
        Assert.Null(actual.FailureRatio);
        Assert.Null(actual.SamplingDuration);
        Assert.Null(actual.TimeoutValue);
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
            ExceptionsAllowedBeforeBreaking = 2,
            FailureRatio = 3.0D,
            SamplingDuration = 4,
            TimeoutValue = 5,
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
            DurationOfBreak = 1,
            ExceptionsAllowedBeforeBreaking = 2,
            FailureRatio = 3.0D,
            SamplingDuration = 4,
            TimeoutValue = 5,
        };
        QoSOptions from = new QoSOptionsBuilder()
            .WithDurationOfBreak(1)
            .WithExceptionsAllowedBeforeBreaking(2)
            .WithFailureRatio(3.0D)
            .WithSamplingDuration(4)
            .WithTimeoutValue(5)
            .Build();

        // Act
        FileQoSOptions actual = new(from); // copying

        // Assert
        Assert.Equivalent(expected, actual);
        AssertEquality(actual, expected);
    }

    private static void AssertEquality(FileQoSOptions actual, FileQoSOptions expected)
    {
        Assert.Equal(expected.DurationOfBreak, actual.DurationOfBreak);
        Assert.Equal(expected.ExceptionsAllowedBeforeBreaking, actual.ExceptionsAllowedBeforeBreaking);
        Assert.Equal(expected.FailureRatio, actual.FailureRatio);
        Assert.Equal(expected.SamplingDuration, actual.SamplingDuration);
        Assert.Equal(expected.TimeoutValue, actual.TimeoutValue);
    }
}
