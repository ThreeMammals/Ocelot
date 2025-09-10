using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.RateLimiting;

public class FileRateLimitByHeaderRuleTests
{
    [Fact]
    public void Ctor_Parameterless()
    {
        // Arrange, Act
        FileRateLimitByHeaderRule actual = new();

        // Assert
        Assert.Null(actual.ClientIdHeader);
        Assert.Null(actual.ClientWhitelist);
    }

    [Fact]
    public void Ctor_CopyingFrom_FileRateLimitRule()
    {
        // Arrange
        FileRateLimitRule from = new()
        {
            EnableHeaders = false,
            EnableRateLimiting = true,
            KeyPrefix = "3",
            Limit = 4,
            Period = "5",
            PeriodTimespan = 6D,
            QuotaMessage = "7",
            StatusCode = 8,
            Wait = "9",
        };

        // Act
        FileRateLimitByHeaderRule actual = new(from);
        FileRateLimitRule actualRule = actual;

        // Assert
        Assert.False(ReferenceEquals(from, actual));
        Assert.Equivalent(from, actualRule);
        Assert.Null(actual.ClientWhitelist);
    }

    [Fact]
    public void Ctor_CopyingFrom_FileRateLimitByHeaderRule()
    {
        // Arrange
        FileRateLimitByHeaderRule from = new()
        {
            ClientIdHeader = "1",
            ClientWhitelist = ["2"],
            DisableRateLimitHeaders = true,
            HttpStatusCode = 4,
            QuotaExceededMessage = "5",
            RateLimitCounterPrefix = "6",
        };

        // Act
        FileRateLimitByHeaderRule actual = new(from);

        // Assert
        Assert.False(ReferenceEquals(from, actual));
        Assert.Equivalent(from, actual);
    }

    [Fact]
    public void ToString_DisabledRateLimiting_ShouldBeEmpty()
    {
        // Arrange
        FileRateLimitByHeaderRule rule = new()
        {
            EnableRateLimiting = false,
        };

        // Act
        var actual = rule.ToString();

        // Assert
        Assert.Empty(actual);
    }

    [Theory]
    [InlineData(null, "H+:3:2s:w1s/HDR:hdr/WL[client1]")]
    [InlineData(false, "H+:3:2s:w1s/HDR:hdr/WL[client1]")]
    [InlineData(true, "H-:3:2s:w1s/HDR:hdr/WL[client1]")]
    public void ToString_DisableRateLimitHeaders(bool? disableRateLimitHeaders, string expected)
    {
        // Format: <c>H{+,-}:{limit}:{period}:w{wait}/HDR:{client_id_header}/WL[{c1,c2,...}]</c>.
        // Arrange
        var rule = GivenRule();
        rule.DisableRateLimitHeaders = disableRateLimitHeaders;

        // Act
        var actual = rule.ToString();

        // Assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(null, "H+:3:2s:w1s/HDR:hdr/WL-")]
    [InlineData(true, "H+:3:2s:w1s/HDR:hdr/WL[]")]
    [InlineData(false, "H+:3:2s:w1s/HDR:hdr/WL[cl1,cl2]")]
    public void ToString_ClientWhitelist(bool? isEmpty, string expected)
    {
        // Format: <c>H{+,-}:{limit}:{period}:w{wait}/HDR:{client_id_header}/WL[{c1,c2,...}]</c>.
        // Arrange
        var rule = GivenRule();
        rule.ClientWhitelist = isEmpty switch
        {
            null => null,
            true => [],
            false => ["cl1", "cl2"],
        };

        // Act
        var actual = rule.ToString();

        // Assert
        Assert.Equal(expected, actual);
    }

    private static FileRateLimitByHeaderRule GivenRule() => new()
    {
        Limit = 3,
        Period = "2s",
        Wait = "1s",
        ClientIdHeader = "hdr",
        ClientWhitelist = ["client1"],
    };
}
