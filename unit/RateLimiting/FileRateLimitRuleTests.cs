using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.RateLimiting;

public class FileRateLimitRuleTests
{
    [Fact]
    public void Ctor_Copying_ArgCheck()
    {
        // Arrange, Act
        var ex = Assert.Throws<ArgumentNullException>(() => new FileRateLimitRule(null));

        // Assert
        Assert.Equal(ex.ParamName, "from");
    }

    [Fact]
    public void Ctor_Copying_Copied()
    {
        // Arrange
        FileRateLimitRule from = new()
        {
            EnableRateLimiting = true,
            EnableHeaders = true,
            Limit = 3,
            Period = "4s",
            PeriodTimespan = 5D,
            Wait = "6s",
            StatusCode = 7,
            QuotaMessage = "8",
            KeyPrefix = "9",
        };

        // Act
        var actual = new FileRateLimitRule(from);

        // Assert
        Assert.NotNull(actual);
        Assert.False(ReferenceEquals(from, actual));
        Assert.Equivalent(from, actual);
    }

    [Fact]
    [Trait("Feat", "1229")] // https://github.com/ThreeMammals/Ocelot/issues/1229
    [Trait("PR", "2294")] // https://github.com/ThreeMammals/Ocelot/pull/2294
    public void ToString_DisabledRateLimiting_ShouldBeEmpty()
    {
        // Arrange
        FileRateLimitRule rule = new()
        {
            EnableRateLimiting = false,
        };

        // Act
        var actual = rule.ToString();

        // Assert
        Assert.Empty(actual);
    }

    [Fact]
    [Trait("Feat", "1229")]
    [Trait("PR", "2294")]
    public void ToString_HappyPath()
    {
        // Arrange
        FileRateLimitRule rule = new()
        {
            Limit = 3,
            Period = "1s",
            Wait = "2s",
        };

        // Act
        var actual = rule.ToString();

        // Assert
        Assert.Equal("H+:3:1s:w2s", actual);
    }

    [Theory]
    [Trait("Feat", "1229")]
    [Trait("PR", "2294")]
    [InlineData(null, "H+:::w-")]
    [InlineData(true, "H+:::w-")]
    [InlineData(false, "H-:::w-")]
    public void ToString_EnableHeaders(bool? enableHeaders, string expected)
    {
        // Arrange
        FileRateLimitRule rule = new()
        {
            EnableHeaders = enableHeaders,
        };

        // Act
        var actual = rule.ToString();

        // Assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [Trait("Feat", "1229")]
    [Trait("PR", "2294")]
    [InlineData(null, "H+:::w-")]
    [InlineData(1.234D, "H+:::w1.234s")]
    public void ToString_PeriodTimespan(double? periodTimespan, string expected)
    {
        // Arrange
        FileRateLimitRule rule = new()
        {
            PeriodTimespan = periodTimespan,
        };

        // Act
        var actual = rule.ToString();

        // Assert
        Assert.Equal(expected, actual);
    }
}
