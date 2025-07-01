using Ocelot.Configuration;
using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Configuration;

public class QoSOptionsTests
{
    [Fact]
    public void Ctor_Copy_ShouldCopy()
    {
        // Arrange
        var copyee = new QoSOptions(
            exceptionsAllowedBeforeBreaking: 1,
            durationOfBreak: 2,
            timeoutValue: 3,
            key: "123");

        // Act
        var actual = new QoSOptions(copyee);

        // Assert
        Assert.Equivalent(copyee, actual);
        Assert.Equal(copyee.ExceptionsAllowedBeforeBreaking, actual.ExceptionsAllowedBeforeBreaking);
        Assert.Equal(copyee.DurationOfBreak, actual.DurationOfBreak);
        Assert.Equal(copyee.TimeoutValue, actual.TimeoutValue);
        Assert.Equal(copyee.Key, actual.Key);
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
        var opts = new QoSOptions(exceptionsAllowed, 0, null, null); // timeoutValue is null

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
        var opts = new QoSOptions(0, 0, timeout, null); // no exceptionsAllowedBeforeBreaking

        // Act, Assert
        Assert.Equal(expected, opts.UseQos);
    }
}
