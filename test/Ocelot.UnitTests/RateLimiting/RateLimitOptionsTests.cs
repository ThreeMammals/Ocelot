using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using System.Data;
using System.Net;
using System.Net.Mail;

namespace Ocelot.UnitTests.RateLimiting;

public class RateLimitOptionsTests
{
    [Theory]
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
}
