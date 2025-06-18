using Ocelot.Configuration;

namespace Ocelot.UnitTests.Configuration;

public class QoSOptionsTests
{
    [Theory]
    [InlineData(0, QoSOptions.DefTimeout)] // not in range
    [InlineData(QoSOptions.LowTimeout - 1, QoSOptions.DefTimeout)] // not in range
    [InlineData(QoSOptions.LowTimeout, QoSOptions.DefTimeout)] // not in range
    [InlineData(QoSOptions.LowTimeout + 1, QoSOptions.LowTimeout + 1)] // in range
    [InlineData(QoSOptions.DefTimeout, QoSOptions.DefTimeout)] // in range
    [InlineData(QoSOptions.HighTimeout - 1, QoSOptions.HighTimeout - 1)] // in range
    [InlineData(QoSOptions.HighTimeout, QoSOptions.DefTimeout)] // not in range
    [InlineData(QoSOptions.HighTimeout + 1, QoSOptions.DefTimeout)] // not in range
    public void DefaultTimeout_Setter_ShouldBeGreaterThan10AndLessThan24hours(int value, int expected)
    {
        // Arrange, Act
        QoSOptions.DefaultTimeout = value;

        // Assert
        Assert.Equal(expected, QoSOptions.DefaultTimeout);
    }
}
