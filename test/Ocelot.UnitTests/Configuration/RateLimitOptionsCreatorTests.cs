using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Configuration;

public class RateLimitOptionsCreatorTests : UnitTest
{
    private readonly RateLimitOptionsCreator _creator = new();

    [Fact]
    public void Should_create_rate_limit_options()
    {
        // Arrange
        var fileRoute = new FileRoute
        {
            RateLimitOptions = new FileRateLimitRule
            {
                ClientWhitelist = new List<string>(),
                Period = "Period",
                Limit = 1,
                PeriodTimespan = 1,
                EnableRateLimiting = true,
            },
        };
        var fileGlobalConfig = new FileGlobalConfiguration
        {
            RateLimitOptions = new FileRateLimitOptions
            {
                ClientIdHeader = "ClientIdHeader",
                DisableRateLimitHeaders = true,
                QuotaExceededMessage = "QuotaExceededMessage",
                RateLimitCounterPrefix = "RateLimitCounterPrefix",
                HttpStatusCode = 200,
            },
        };
        var expected = new RateLimitOptionsBuilder()
            .WithClientIdHeader("ClientIdHeader")
            .WithClientWhiteList(() => fileRoute.RateLimitOptions.ClientWhitelist)
            .WithDisableRateLimitHeaders(true)
            .WithEnableRateLimiting(true)
            .WithHttpStatusCode(200)
            .WithQuotaExceededMessage("QuotaExceededMessage")
            .WithRateLimitCounterPrefix("RateLimitCounterPrefix")
            .WithRateLimitRule(new RateLimitRule(fileRoute.RateLimitOptions.Period,
                   fileRoute.RateLimitOptions.PeriodTimespan,
                   fileRoute.RateLimitOptions.Limit))
            .Build();
        bool enabled = true;

        // Act
        var result = _creator.Create(fileRoute, fileGlobalConfig);

        // Assert
        enabled.ShouldBeTrue();
        result.ClientIdHeader.ShouldBe(expected.ClientIdHeader);
        result.ClientWhitelist.ShouldBe(expected.ClientWhitelist);
        result.DisableRateLimitHeaders.ShouldBe(expected.DisableRateLimitHeaders);
        result.EnableRateLimiting.ShouldBe(expected.EnableRateLimiting);
        result.HttpStatusCode.ShouldBe(expected.HttpStatusCode);
        result.QuotaExceededMessage.ShouldBe(expected.QuotaExceededMessage);
        result.RateLimitCounterPrefix.ShouldBe(expected.RateLimitCounterPrefix);
        result.RateLimitRule.Limit.ShouldBe(expected.RateLimitRule.Limit);
        result.RateLimitRule.Period.ShouldBe(expected.RateLimitRule.Period);
        TimeSpan.FromSeconds(result.RateLimitRule.PeriodTimespan).Ticks.ShouldBe(TimeSpan.FromSeconds(expected.RateLimitRule.PeriodTimespan).Ticks);
    }
}
