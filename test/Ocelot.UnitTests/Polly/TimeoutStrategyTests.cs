using Ocelot.Provider.Polly;
using Const = Ocelot.Provider.Polly.TimeoutStrategy;

namespace Ocelot.UnitTests.Polly;

public class TimeoutStrategyTests
{
    [Theory]
    [InlineData(0, Const.DefTimeout)] // not in range
    [InlineData(Const.LowTimeout - 1, Const.DefTimeout)] // not in range
    [InlineData(Const.LowTimeout, Const.DefTimeout)] // not in range
    [InlineData(Const.LowTimeout + 1, Const.LowTimeout + 1)] // in range
    [InlineData(Const.DefTimeout, Const.DefTimeout)] // in range
    [InlineData(Const.HighTimeout - 1, Const.HighTimeout - 1)] // in range
    [InlineData(Const.HighTimeout, Const.DefTimeout)] // not in range
    [InlineData(Const.HighTimeout + 1, Const.DefTimeout)] // not in range
    public void DefaultTimeout_Setter_ShouldBeGreaterThan10msAndLessThan24hours(int value, int expected)
    {
        // Arrange, Act
        TimeoutStrategy.DefaultTimeout = value;

        // Assert
        Assert.Equal(expected, TimeoutStrategy.DefaultTimeout);
    }
}
