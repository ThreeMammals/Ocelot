using Ocelot.Configuration;

namespace Ocelot.UnitTests.Configuration;

[Trait("PR", "2294")] // https://github.com/ThreeMammals/Ocelot/pull/2294
public class RateLimitRuleTests
{
    [Theory]
    [Trait("Feat", "585")] // https://github.com/ThreeMammals/Ocelot/issues/585
    [Trait("Feat", "1915")] // https://github.com/ThreeMammals/Ocelot/issues/1915
    [InlineData("1ms", 1D)]
    [InlineData("1s", 1_000D)]
    [InlineData("1m", 60_000D)]
    [InlineData("1h", 3_600_000D)]
    [InlineData("1d", 86_400_000D)]
    public void ParseTimespan_ShouldParseSupportedUnits(string oneUnit, double expected)
    {
        TimeSpan expectedSpan = TimeSpan.FromMilliseconds(expected);
        TimeSpan actual = RateLimitRule.ParseTimespan(oneUnit);
        Assert.Equal(expectedSpan, actual);
    }

    [Theory]
    [Trait("Feat", "585")]
    [Trait("Feat", "1915")]
    [InlineData("1ms", 1.0D)]
    [InlineData("123ms", 123.0D)]
    [InlineData("2.ms", 2.0D)]
    [InlineData("3.0ms", 3.0D)]
    [InlineData(".4ms", 0.4D)]
    [InlineData("0.5ms", 0.5D)]
    [InlineData("0.678ms", 0.678D)]
    [InlineData("123.456ms", 123.456D)]
    public void ParseTimespan_ShouldParseMilliseconds(string value, double ms)
    {
        TimeSpan expected = TimeSpan.FromMilliseconds(ms);
        TimeSpan actual = RateLimitRule.ParseTimespan(value);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [Trait("Feat", "585")]
    [Trait("Feat", "1915")]
    [InlineData("1", 1.0D)]
    [InlineData("123", 123.0D)]
    [InlineData("2.", 2.0D)]
    [InlineData("3.0", 3.0D)]
    [InlineData(".4", 0.4D)]
    [InlineData("0.5", 0.5D)]
    [InlineData("0.678", 0.678D)]
    [InlineData("78.999", 78.999D)]
    public void ParseTimespan_ShouldParseWithoutUnitToMilliseconds(string value, double ms)
    {
        TimeSpan expected = TimeSpan.FromMilliseconds(ms);
        TimeSpan actual = RateLimitRule.ParseTimespan(value);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [Trait("Feat", "585")]
    [Trait("Feat", "1915")]
    [InlineData("-1ms", 1D)]
    [InlineData("-20s", 20_000D)]
    [InlineData("-3.0m", 180_000D)]
    public void ParseTimespan_ShouldParseNegativeAsPositive(string value, double ms)
    {
        TimeSpan expected = TimeSpan.FromMilliseconds(ms);
        TimeSpan actual = RateLimitRule.ParseTimespan(value);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [Trait("Feat", "585")]
    [Trait("Feat", "1915")]
    [InlineData("1x", "The '1x' timespan cannot be converted to TimeSpan due to an unknown 'x' unit!")]
    [InlineData("x-bla-bla", $"The 'x-bla-bla' value doesn't include any digits, so it cannot be considered a number!")]
    public void ParseTimespan_ShouldThrowFormatException(string value, string message)
    {
        var error = Assert.Throws<FormatException>(() => RateLimitRule.ParseTimespan(value));
        Assert.Equal(message, error.Message);
    }
}
