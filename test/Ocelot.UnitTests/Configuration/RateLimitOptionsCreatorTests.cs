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
                QuotaExceededMessage = "QuotaMessage",
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
            QuotaMessage = "QuotaMessage",
            KeyPrefix = "RateLimitCounterPrefix",
            Rule = new(fileRoute.RateLimitOptions.Period, fileRoute.RateLimitOptions.Wait, fileRoute.RateLimitOptions.Limit.Value),
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
        result.QuotaMessage.ShouldBe(expected.QuotaMessage);
        result.KeyPrefix.ShouldBe(expected.KeyPrefix);
        result.Rule.Limit.ShouldBe(expected.Rule.Limit);
        result.Rule.Period.ShouldBe(expected.Rule.Period);
        result.Rule.Wait.ShouldBe(expected.Rule.Wait);
    }
}
