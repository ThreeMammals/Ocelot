using Ocelot.Configuration;
using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Configuration;

public class QoSOptionsTests
{
    [Fact]
    public void UseQos_NoOptions_ShouldNotUse()
    {
        // Arrange
        var from = new FileQoSOptions();
        var opts = new QoSOptions(from);

        // Act, Assert
        Assert.False(opts.UseQos);
    }

    [Theory]
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
