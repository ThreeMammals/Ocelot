using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Configuration;

public class QoSOptionsTests
{
    [Fact]
    public void Ctor_Copy_ShouldCopy()
    {
        // Arrange
        var copyee = new QoSOptionsBuilder()
            .WithExceptionsAllowedBeforeBreaking(1)
            .WithDurationOfBreak(2)
            .WithTimeoutValue(3)
            .WithFailureRatio(4.0D)
            .WithSamplingDuration(5)
            .Build();

        // Act
        var actual = new QoSOptions(copyee);

        // Assert
        Assert.Equivalent(copyee, actual);
        Assert.Equal(copyee.ExceptionsAllowedBeforeBreaking, actual.ExceptionsAllowedBeforeBreaking);
        Assert.Equal(copyee.DurationOfBreak, actual.DurationOfBreak);
        Assert.Equal(copyee.TimeoutValue, actual.TimeoutValue);
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
        var opts = new QoSOptionsBuilder()
            .WithExceptionsAllowedBeforeBreaking(exceptionsAllowed)
            .Build(); // timeoutValue is null

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
        var opts = new QoSOptionsBuilder()
            .WithTimeoutValue(timeout)
            .Build(); // no exceptionsAllowedBeforeBreaking

        // Act, Assert
        Assert.Equal(expected, opts.UseQos);
    }
}
