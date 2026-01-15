using Ocelot.Provider.Polly;
using Const = Ocelot.Provider.Polly.CircuitBreakerStrategy;

namespace Ocelot.UnitTests.Polly;

public class CircuitBreakerStrategyTests
{
    [Theory]
    [Trait("PR", "2073")]
    [InlineData(0, Const.DefaultBreakDuration)] // out of range
    [InlineData(500, Const.DefaultBreakDuration)] // out of range
    [InlineData(501, 501)] // in range
    [InlineData(Const.DefaultBreakDuration, Const.DefaultBreakDuration)] // in range
    [InlineData(86_400_000 - 1, 86_400_000 - 1)] // in range
    [InlineData(86_400_000, Const.DefaultBreakDuration)] // out of range
    public void BreakDuration_ShouldBeInRange(int ms, int expected)
    {
        // Arrange, Act
        var actual = CircuitBreakerStrategy.BreakDuration(ms);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [Trait("PR", "2073")]
    [InlineData(0, Const.DefaultMinimumThroughput)] // out of range
    [InlineData(1, Const.DefaultMinimumThroughput)] // out of range
    [InlineData(2, 2)] // in range
    [InlineData(Const.DefaultMinimumThroughput, Const.DefaultMinimumThroughput)] // in range
    public void MinimumThroughput_ShouldBeTwoOrGreater(int value, int expected)
    {
        // Arrange, Act
        var actual = CircuitBreakerStrategy.MinimumThroughput(value);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [Trait("PR", "2081")]
    [Trait("Feat", "2080")]
    [InlineData(0.0D, Const.DefaultFailureRatio)] // out of range
    [InlineData(0.05D, 0.05D)] // in range
    [InlineData(Const.DefaultFailureRatio, Const.DefaultFailureRatio)] // in range
    [InlineData(0.99D, 0.99D)] // in range
    [InlineData(1.0, Const.DefaultFailureRatio)] // out of range
    public void FailureRatio_ShouldBeInRange(double ratio, double expected)
    {
        // Arrange, Act
        var actual = CircuitBreakerStrategy.FailureRatio(ratio);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [Trait("PR", "2081")]
    [Trait("Feat", "2080")]
    [InlineData(0, Const.DefaultSamplingDuration)] // out of range
    [InlineData(500, Const.DefaultSamplingDuration)] // out of range
    [InlineData(501, 501)] // in range
    [InlineData(Const.DefaultSamplingDuration, Const.DefaultSamplingDuration)] // in range
    [InlineData(86_400_000 - 1, 86_400_000 - 1)] // in range
    [InlineData(86_400_000, Const.DefaultSamplingDuration)] // out of range
    public void SamplingDuration_ShouldBeInRange(int ms, int expected)
    {
        // Arrange, Act
        var actual = CircuitBreakerStrategy.SamplingDuration(ms);

        // Assert
        Assert.Equal(expected, actual);
    }
}
