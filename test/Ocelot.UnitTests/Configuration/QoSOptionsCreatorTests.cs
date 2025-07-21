using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Configuration;

public class QoSOptionsCreatorTests : UnitTest
{
    private readonly QoSOptionsCreator _creator = new();

    [Fact]
    public void ShouldCreateQosOptions()
    {
        // Arrange
        var route = new FileRoute
        {
            QoSOptions = new FileQoSOptions
            {
                DurationOfBreak = 1,
                ExceptionsAllowedBeforeBreaking = 2,
                FailureRatio = 3.0D,
                SamplingDuration = 4,
                TimeoutValue = 5,
            },
        };
        var expected = new QoSOptionsBuilder()
            .WithDurationOfBreak(1)
            .WithExceptionsAllowedBeforeBreaking(2)
            .WithFailureRatio(3.0D)
            .WithSamplingDuration(4)
            .WithTimeoutValue(5)
            .Build();

        // Act
        var actual = _creator.Create(route.QoSOptions);

        // Assert
        AssertEquality(actual, expected);
    }

    [Fact]
    [Trait("PR", "2081")]
    [Trait("Feat", "2080")]
    public void NoRouteOptions_ShouldCreateFromGlobalQosOptions()
    {
        // Arrange
        FileGlobalConfiguration global = new()
        {
            QoSOptions = new()
            {
                DurationOfBreak = 1,
                ExceptionsAllowedBeforeBreaking = 2,
                FailureRatio = 3.0D,
                SamplingDuration = 4,
                TimeoutValue = 5,
            },
        };
        FileRoute route = new();
        QoSOptions expected = new(global.QoSOptions);

        // Act
        var actual = _creator.Create(route, global);

        // Assert
        Assert.Equivalent(expected, actual);
        AssertEquality(actual, expected);
    }

    [Fact]
    [Trait("PR", "2081")]
    [Trait("Feat", "2080")]
    public void HasRouteOptions_ShouldCreateFromRouteQosOptions()
    {
        // Arrange
        FileGlobalConfiguration global = new()
        {
            QoSOptions = new()
            {
                DurationOfBreak = 1,
                ExceptionsAllowedBeforeBreaking = 2,
                FailureRatio = 3.0D,
                SamplingDuration = 4,
                TimeoutValue = 5,
            },
        };
        FileRoute route = new()
        {
            QoSOptions = new FileQoSOptions
            {
                DurationOfBreak = 10,
                ExceptionsAllowedBeforeBreaking = 20,
                FailureRatio = 30.0D,
                SamplingDuration = 40,
                TimeoutValue = 50,
            },
        };
        QoSOptions expected = new(route.QoSOptions);

        // Act
        var actual = _creator.Create(route, global);

        // Assert
        Assert.Equivalent(expected, actual);
        AssertEquality(actual, expected);
    }

    private static void AssertEquality(QoSOptions actual, QoSOptions expected)
    {
        Assert.Equal(expected.DurationOfBreak, actual.DurationOfBreak);
        Assert.Equal(expected.ExceptionsAllowedBeforeBreaking, actual.ExceptionsAllowedBeforeBreaking);
        Assert.Equal(expected.FailureRatio, actual.FailureRatio);
        Assert.Equal(expected.SamplingDuration, actual.SamplingDuration);
        Assert.Equal(expected.TimeoutValue, actual.TimeoutValue);
    }
}
