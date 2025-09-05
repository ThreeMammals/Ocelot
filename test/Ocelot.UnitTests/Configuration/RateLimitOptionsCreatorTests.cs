using Ocelot.Configuration;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.RateLimiting;

namespace Ocelot.UnitTests.Configuration;

public class RateLimitOptionsCreatorTests : UnitTest
{
    private readonly RateLimitOptionsCreator _creator = new(Mock.Of<IRateLimiting>());

    [Fact]
    public void Should_create_rate_limit_options()
    {
        // Arrange
        var fileRoute = new FileRoute
        {
            RateLimitOptions = new FileRateLimitByHeaderRule
            {
                ClientWhitelist = new List<string>(),
                Period = "Period",
                Limit = 1,
                Wait = "OneSecond",
                EnableRateLimiting = true,
            },
        };
        var fileGlobalConfig = new FileGlobalConfiguration
        {
            RateLimitOptions = new()
            {
                ClientIdHeader = "ClientIdHeader",
                EnableHeaders = true,
                QuotaExceededMessage = "QuotaExceededMessage",
                RateLimitCounterPrefix = "RateLimitCounterPrefix",
                HttpStatusCode = 200,
            },
        };
        RateLimitOptions expected = new()
        {
            ClientIdHeader = "ClientIdHeader",
            ClientWhitelist = fileRoute.RateLimitOptions.ClientWhitelist,
            EnableHeaders = true,
            EnableRateLimiting = true,
            StatusCode = 200,
            QuotaExceededMessage = "QuotaExceededMessage",
            RateLimitCounterPrefix = "RateLimitCounterPrefix",
            RateLimitRule = new(fileRoute.RateLimitOptions.Period, fileRoute.RateLimitOptions.Wait, fileRoute.RateLimitOptions.Limit.Value),
        };
        bool enabled = true;

        // Act
        var result = _creator.Create(fileRoute, fileGlobalConfig);

        // Assert
        enabled.ShouldBeTrue();
        result.ClientIdHeader.ShouldBe(expected.ClientIdHeader);
        result.ClientWhitelist.ShouldBe(expected.ClientWhitelist);
        result.EnableHeaders.ShouldBe(expected.EnableHeaders);
        result.EnableRateLimiting.ShouldBe(expected.EnableRateLimiting);
        result.StatusCode.ShouldBe(expected.StatusCode);
        result.QuotaExceededMessage.ShouldBe(expected.QuotaExceededMessage);
        result.RateLimitCounterPrefix.ShouldBe(expected.RateLimitCounterPrefix);
        result.RateLimitRule.Limit.ShouldBe(expected.RateLimitRule.Limit);
        result.RateLimitRule.Period.ShouldBe(expected.RateLimitRule.Period);
        result.RateLimitRule.Wait.ShouldBe(expected.RateLimitRule.Wait);
    }
}
