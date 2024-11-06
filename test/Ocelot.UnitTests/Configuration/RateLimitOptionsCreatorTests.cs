﻿using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Configuration
{
    public class RateLimitOptionsCreatorTests : UnitTest
    {
        private FileRoute _fileRoute;
        private FileGlobalConfiguration _fileGlobalConfig;
        private bool _enabled;
        private readonly RateLimitOptionsCreator _creator;
        private RateLimitOptions _result;

        public RateLimitOptionsCreatorTests()
        {
            _creator = new RateLimitOptionsCreator();
        }

        [Fact]
        public void should_create_rate_limit_options_ocelot()
        {
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
                .WithRateLimitMiddlewareType(RateLimitMiddlewareType.Ocelot)
                .Build();

            _enabled = false;

            this.Given(x => x.GivenTheFollowingFileRoute(fileRoute))
                .And(x => x.GivenTheFollowingFileGlobalConfig(fileGlobalConfig))
                .And(x => x.GivenRateLimitingIsEnabled())
                .When(x => x.WhenICreate())
                .Then(x => x.ThenTheFollowingIsReturned(expected))
                .BDDfy();
        }
        
        [Fact]
        public void should_create_rate_limit_options_dotnet()
        {
            var fileRoute = new FileRoute
            {
                RateLimitOptions = new FileRateLimitRule
                {
                    RateLimitMiddlewareType = RateLimitMiddlewareType.DotNet,
                    RateLimitPolicyName = "test",
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
                    HttpStatusCode = 200,
                },
            };
            var expected = new RateLimitOptionsBuilder()
                .WithClientIdHeader("ClientIdHeader")
                .WithClientWhiteList(() => new List<string>())
                .WithDisableRateLimitHeaders(true)
                .WithEnableRateLimiting(true)
                .WithHttpStatusCode(200)
                .WithQuotaExceededMessage("QuotaExceededMessage")
                .WithRateLimitCounterPrefix("ocelot")
                .WithRateLimitRule(new RateLimitRule(null, 0, 0))
                .WithRateLimitMiddlewareType(RateLimitMiddlewareType.DotNet)
                .WithRateLimitPolicyName("test")
                .Build();

            _enabled = false;

            this.Given(x => x.GivenTheFollowingFileRoute(fileRoute))
                .And(x => x.GivenTheFollowingFileGlobalConfig(fileGlobalConfig))
                .And(x => x.GivenRateLimitingIsEnabled())
                .When(x => x.WhenICreate())
                .Then(x => x.ThenTheFollowingIsReturned(expected))
                .BDDfy();
        }

        private void GivenTheFollowingFileRoute(FileRoute fileRoute)
        {
            _fileRoute = fileRoute;
        }

        private void GivenTheFollowingFileGlobalConfig(FileGlobalConfiguration fileGlobalConfig)
        {
            _fileGlobalConfig = fileGlobalConfig;
        }

        private void GivenRateLimitingIsEnabled()
        {
            _enabled = true;
        }

        private void WhenICreate()
        {
            _result = _creator.Create(_fileRoute.RateLimitOptions, _fileGlobalConfig);
        }

        private void ThenTheFollowingIsReturned(RateLimitOptions expected)
        {
            _enabled.ShouldBeTrue();
            _result.ClientIdHeader.ShouldBe(expected.ClientIdHeader);
            _result.ClientWhitelist.ShouldBe(expected.ClientWhitelist);
            _result.DisableRateLimitHeaders.ShouldBe(expected.DisableRateLimitHeaders);
            _result.EnableRateLimiting.ShouldBe(expected.EnableRateLimiting);
            _result.HttpStatusCode.ShouldBe(expected.HttpStatusCode);
            _result.QuotaExceededMessage.ShouldBe(expected.QuotaExceededMessage);
            _result.RateLimitCounterPrefix.ShouldBe(expected.RateLimitCounterPrefix);
            _result.RateLimitRule.Limit.ShouldBe(expected.RateLimitRule.Limit);
            _result.RateLimitRule.Period.ShouldBe(expected.RateLimitRule.Period);
            TimeSpan.FromSeconds(_result.RateLimitRule.PeriodTimespan).Ticks.ShouldBe(TimeSpan.FromSeconds(expected.RateLimitRule.PeriodTimespan).Ticks);
        }
    }
}
