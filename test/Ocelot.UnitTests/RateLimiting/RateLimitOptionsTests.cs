using Ocelot.Configuration;
using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.RateLimiting;

public class RateLimitOptionsTests
{
    [Fact]
    public void Ctor_Parameterless()
    {
        // Arrange, Act
        RateLimitOptions actual = new();

        // Assert
        Assert.Equal("Oc-Client", actual.ClientIdHeader);
        Assert.Empty(actual.ClientWhitelist);
        Assert.True(actual.EnableHeaders);
        Assert.True(actual.EnableRateLimiting);
        Assert.Equal(429, actual.StatusCode);
        Assert.Equal("API calls quota exceeded! Maximum admitted {0} per {1}.", actual.QuotaMessage);
        Assert.Equal("Ocelot.RateLimiting", actual.KeyPrefix);
        Assert.Equal(RateLimitRule.Empty, actual.Rule);
    }

    [Theory]
    [Trait("Feat", "1229")]
    [Trait("PR", "2294")]
    [InlineData(false)]
    [InlineData(true)]
    public void Ctor_Boolean(bool enableRateLimiting)
    {
        // Arrange, Act
        RateLimitOptions actual = new(enableRateLimiting);

        // Assert
        Assert.Equal(enableRateLimiting, actual.EnableRateLimiting);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Ctor_Initialization(bool isEmpty)
    {
        // Arrange
        string[] clientIdHeader = new[] { nameof(RateLimitOptions.ClientIdHeader), RateLimitOptions.DefaultClientHeader };
        List<string>[] clientWhitelist = new[] { new List<string>([nameof(RateLimitOptions.ClientWhitelist)]), default };
        bool[] enableHeaders = new[] { true, default };
        bool[] enableRateLimiting = new[] { true, default };
        string[] rateLimitCounterPrefix = new[] { nameof(RateLimitOptions.KeyPrefix), RateLimitOptions.DefaultCounterPrefix };
        string[] quotaExceededMessage = new[] { nameof(RateLimitOptions.QuotaMessage), RateLimitOptions.DefaultQuotaMessage };
        RateLimitRule[] rateLimitRule = new[] { new RateLimitRule("1", "2", 3), default };
        int[] httpStatusCode = new[] { 404, default };

        // Act
        int i = isEmpty ? 1 : 0;
        RateLimitOptions actual = new(
            enableRateLimiting[i],
            clientIdHeader[i],
            clientWhitelist[i],
            enableHeaders[i],
            quotaExceededMessage[i],
            rateLimitCounterPrefix[i],
            rateLimitRule[i],
            httpStatusCode[i]);

        // Assert
        Assert.Equal(clientIdHeader[i], actual.ClientIdHeader);
        Assert.Equal(clientWhitelist[i] ?? [], actual.ClientWhitelist);
        Assert.Equal(enableHeaders[i], actual.EnableHeaders);
        Assert.Equal(enableRateLimiting[i], actual.EnableRateLimiting);
        Assert.Equal(rateLimitCounterPrefix[i], actual.KeyPrefix);
        Assert.Equal(quotaExceededMessage[i], actual.QuotaMessage);
        Assert.Equal(rateLimitCounterPrefix[i], actual.KeyPrefix);
        Assert.Equal(rateLimitRule[i], actual.Rule);
        Assert.Equal(httpStatusCode[i], actual.StatusCode);
    }

    [Fact]
    [Trait("Feat", "1229")]
    [Trait("PR", "2294")]
    public void Ctor_CopyingFrom_ArgChecks()
    {
        // Arrange
        FileRateLimitByHeaderRule from = null;

        // Act, Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new RateLimitOptions(from));
        Assert.Equal("from", ex.ParamName);
    }

    [Fact]
    [Trait("Feat", "1229")]
    [Trait("PR", "2294")]
    public void Ctor_CopyingFrom_WithObsoleteProps()
    {
        // Arrange
        FileRateLimitByHeaderRule from = new()
        {
            ClientIdHeader = "1",
            ClientWhitelist = ["2"],
            DisableRateLimitHeaders = true,
            EnableRateLimiting = true,
            HttpStatusCode = 333,
            QuotaExceededMessage = "4",
            RateLimitCounterPrefix = "5",
            Period = "6",
            PeriodTimespan = 7D,
            Limit = 8,
        };

        // Act
        RateLimitOptions actual = new(from);

        // Assert
        Assert.Equal("1", actual.ClientIdHeader);
        Assert.Contains("2", actual.ClientWhitelist);
        Assert.False(actual.EnableHeaders);
        Assert.True(actual.EnableRateLimiting);
        Assert.Equal(333, actual.StatusCode);
        Assert.Equal("4", actual.QuotaMessage);
        Assert.Equal("5", actual.KeyPrefix);
        Assert.Equal("8/6/w7s", actual.Rule.ToString());
    }

    [Fact]
    [Trait("Feat", "1229")]
    [Trait("PR", "2294")]
    public void Ctor_CopyingFrom()
    {
        // Arrange
        FileRateLimitByHeaderRule from = new()
        {
            ClientIdHeader = "1",
            ClientWhitelist = ["2"],
            DisableRateLimitHeaders = null,
            EnableHeaders = true,
            EnableRateLimiting = true,
            HttpStatusCode = null,
            StatusCode = 444,
            QuotaExceededMessage = null,
            QuotaMessage = "55",
            RateLimitCounterPrefix = null,
            KeyPrefix = "66",
            Period = "7",
            PeriodTimespan = null,
            Wait = "8",
            Limit = 9,
        };

        // Act
        RateLimitOptions actual = new(from);

        // Assert
        Assert.Equal("1", actual.ClientIdHeader);
        Assert.Contains("2", actual.ClientWhitelist);
        Assert.True(actual.EnableHeaders);
        Assert.True(actual.EnableRateLimiting);
        Assert.Equal(444, actual.StatusCode);
        Assert.Equal("55", actual.QuotaMessage);
        Assert.Equal("66", actual.KeyPrefix);
        Assert.Equal("9/7/w8", actual.Rule.ToString());
    }

    [Fact]
    [Trait("Feat", "1229")]
    [Trait("PR", "2294")]
    public void Ctor_CopyingFrom_WithDefaults()
    {
        // Arrange
        FileRateLimitByHeaderRule from = new();

        // Act
        RateLimitOptions actual = new(from);

        // Assert
        Assert.Equal("Oc-Client", actual.ClientIdHeader);
        Assert.Empty(actual.ClientWhitelist);
        Assert.True(actual.EnableHeaders);
        Assert.True(actual.EnableRateLimiting);
        Assert.Equal(429, actual.StatusCode);
        Assert.Equal("API calls quota exceeded! Maximum admitted {0} per {1}.", actual.QuotaMessage);
        Assert.Equal("Ocelot.RateLimiting", actual.KeyPrefix);
        Assert.Equal(RateLimitRule.Empty.ToString(), actual.Rule.ToString());
    }
}
