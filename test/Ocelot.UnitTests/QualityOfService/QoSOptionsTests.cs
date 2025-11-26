using Ocelot.Configuration;
using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.QualityOfService;

public class QoSOptionsTests
{
    [Fact]
    public void Ctor_Copy_ShouldCopy()
    {
        // Arrange
        var copyee = new QoSOptions(1, 2)
        {
            FailureRatio = 3.0D,
            SamplingDuration = 4,
            Timeout = 5,
        };

        // Act
        var actual = new QoSOptions(copyee);

        // Assert
        Assert.Equivalent(copyee, actual);
        Assert.Equal(copyee.MinimumThroughput, actual.MinimumThroughput);
        Assert.Equal(copyee.BreakDuration, actual.BreakDuration);
        Assert.Equal(copyee.Timeout, actual.Timeout);
        Assert.Equal(copyee.FailureRatio, actual.FailureRatio);
        Assert.Equal(copyee.SamplingDuration, actual.SamplingDuration);
    }

    [Fact]
    [Trait("PR", "2073")]
    public void UseQos_NoOptions_ShouldNotUse()
    {
        // Arrange
        var from = new FileQoSOptions();
        var opts = new QoSOptions(from);

        // Act, Assert
        Assert.False(opts.UseQos);
    }

    [Theory]
    [Trait("PR", "2073")]
    [InlineData(0, false)] // should not use
    [InlineData(1, true)] // should use
    public void UseQos_ExceptionsAllowedBeforeBreaking_ShouldUse(int exceptionsAllowed, bool expected)
    {
        // Arrange
        var opts = new QoSOptions()
        {
            MinimumThroughput = exceptionsAllowed,
        }; // timeoutValue is null

        // Act, Assert
        Assert.Equal(expected, opts.UseQos);
    }

    [Theory]
    [Trait("PR", "2073")]
    [InlineData(null, false)] // should not use
    [InlineData(0, false)] // should not use
    [InlineData(1, true)] // should use
    public void UseQos_TimeoutValue_ShouldUse(int? timeout, bool expected)
    {
        // Arrange
        var opts = new QoSOptions(timeout); // no exceptionsAllowedBeforeBreaking

        // Act, Assert
        Assert.Equal(expected, opts.UseQos);
    }
}
