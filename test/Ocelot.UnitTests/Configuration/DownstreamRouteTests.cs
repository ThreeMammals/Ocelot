using Ocelot.Configuration;

namespace Ocelot.UnitTests.Configuration;

[Collection(nameof(SequentialTests))]
public class DownstreamRouteTests
{
    [Theory]
    [Trait("PR", "2073")]
    [InlineData(0, DownstreamRoute.DefTimeout)] // not in range
    [InlineData(DownstreamRoute.LowTimeout - 1, DownstreamRoute.DefTimeout)] // not in range
    [InlineData(DownstreamRoute.LowTimeout, DownstreamRoute.LowTimeout)] // in range
    [InlineData(DownstreamRoute.LowTimeout + 1, DownstreamRoute.LowTimeout + 1)] // in range
    [InlineData(DownstreamRoute.DefTimeout, DownstreamRoute.DefTimeout)] // in range
    public void DefaultTimeoutSeconds_Setter_ShouldBeGreaterThanOrEqualToThree(int value, int expected)
    {
        // Arrange, Act
        DownstreamRoute.DefaultTimeoutSeconds = value;

        // Assert
        Assert.Equal(expected, DownstreamRoute.DefaultTimeoutSeconds);
        DownstreamRoute.DefaultTimeoutSeconds = DownstreamRoute.DefTimeout; // recover clean state after assembly starting
    }
}
