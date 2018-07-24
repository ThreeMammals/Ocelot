using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Configuration
{
    public class RateLimitOptionsCreatorTests
    {
        private FileReRoute _fileReRoute;
        private FileGlobalConfiguration _fileGlobalConfig;
        private IInternalConfiguration _internalConfig;
        private FileRateLimitOptions _fileRateLimitOptions;
        private bool _enabled;
        private RateLimitOptionsCreator _creator;
        private RateLimitOptions _result;
        private RateLimitGlobalOptions _globalOptionsResult;

        public RateLimitOptionsCreatorTests()
        {
            _creator = new RateLimitOptionsCreator();
        }

        [Fact]
        public void should_create_rate_limit_options()
        {
            var fileReRoute = new FileReRoute
            {
                RateLimitOptions = new FileRateLimitRule
                {
                    ClientWhitelist = new List<string>(),
                    Period = "Period",
                    Limit = 1,
                    PeriodTimespan = 1,
                    EnableRateLimiting = true
                }
            };
            var fileGlobalConfig = new FileGlobalConfiguration
            {
                RateLimitOptions = new FileRateLimitOptions
                {
                    ClientIdHeader = "ClientIdHeader",
                    DisableRateLimitHeaders = true,
                    QuotaExceededMessage = "QuotaExceededMessage",
                    RateLimitCounterPrefix = "RateLimitCounterPrefix",
                    HttpStatusCode = 200
                }
            };
            var expected = new RateLimitOptionsBuilder()
                .WithClientIdHeader("ClientIdHeader")
                .WithClientWhiteList(fileReRoute.RateLimitOptions.ClientWhitelist)
                .WithDisableRateLimitHeaders(true)
                .WithEnableRateLimiting(true)
                .WithHttpStatusCode(200)
                .WithQuotaExceededMessage("QuotaExceededMessage")
                .WithRateLimitCounterPrefix("RateLimitCounterPrefix")
                .WithRateLimitRule(new RateLimitRule(fileReRoute.RateLimitOptions.Period,
                       fileReRoute.RateLimitOptions.PeriodTimespan,
                       fileReRoute.RateLimitOptions.Limit))
                .Build();

            this.Given(x => x.GivenTheFollowingFileReRoute(fileReRoute))
                .And(x => x.GivenTheFollowingFileGlobalConfig(fileGlobalConfig))
                .And(x => x.GivenRateLimitingIsEnabled())
                .When(x => x.WhenICreate())
                .Then(x => x.ThenTheFollowingIsReturned(expected))
                .BDDfy();
        }

        [Fact]
        public void should_create_rate_limit_options_with_internal_config()
        {
            var fileReRoute = new FileReRoute
            {
                RateLimitOptions = new FileRateLimitRule
                {
                    ClientWhitelist = new List<string>(),
                    Period = "Period",
                    Limit = 1,
                    PeriodTimespan = 1,
                    EnableRateLimiting = true
                }
            };

            var rateLimitGlobalOptions = new RateLimitGlobalOptions("ClientIdHeader", true,
                "QuotaExceededMessage", "RateLimitCounterPrefix", 200);

            var internalConfiguration = new InternalConfiguration(null, null,
                null, null, null, null, null, null, rateLimitGlobalOptions, null);

            var expected = new RateLimitOptionsBuilder()
                .WithClientIdHeader(rateLimitGlobalOptions.ClientIdHeader)
                .WithClientWhiteList(fileReRoute.RateLimitOptions.ClientWhitelist)
                .WithDisableRateLimitHeaders(rateLimitGlobalOptions.DisableRateLimitHeaders)
                .WithEnableRateLimiting(true)
                .WithHttpStatusCode(rateLimitGlobalOptions.HttpStatusCode)
                .WithQuotaExceededMessage(rateLimitGlobalOptions.QuotaExceededMessage)
                .WithRateLimitCounterPrefix(rateLimitGlobalOptions.RateLimitCounterPrefix)
                .WithRateLimitRule(new RateLimitRule(fileReRoute.RateLimitOptions.Period,
                       fileReRoute.RateLimitOptions.PeriodTimespan,
                       fileReRoute.RateLimitOptions.Limit))
                .Build();

            this.Given(x => x.GivenTheFollowingFileReRoute(fileReRoute))
                .And(x => x.GivenTheFollowingInternalConfig(internalConfiguration))
                .And(x => x.GivenRateLimitingIsEnabled())
                .When(x => x.WhenICreateWithInternalConfig())
                .Then(x => x.ThenTheFollowingIsReturned(expected))
                .BDDfy();
        }

        [Fact]
        public void should_create_rate_limit_options_with_only_global_options()
        {
            var fileReRoute = new FileReRoute();

            var fileRateLimitOptions = new FileRateLimitOptions
            {
                ClientIdHeader = "ClientIdHeader",
                DisableRateLimitHeaders = true,
                HttpStatusCode = 200,
                QuotaExceededMessage = "QuotaExceededMessage",
                RateLimitCounterPrefix = "RateLimitCounterPrefix"
            };

            var expected = new RateLimitGlobalOptionsBuilder()
                .WithClientIdHeader(fileRateLimitOptions.ClientIdHeader)
                .WithDisableRateLimitHeaders(fileRateLimitOptions.DisableRateLimitHeaders)
                .WithHttpStatusCode(fileRateLimitOptions.HttpStatusCode)
                .WithQuotaExceededMessage(fileRateLimitOptions.QuotaExceededMessage)
                .WithRateLimitCounterPrefix(fileRateLimitOptions.RateLimitCounterPrefix)
                .Build();

            this.Given(x => x.GivenTheFollowingGlobalOptions(fileRateLimitOptions))
                .When(x => x.WhenICreateWithGlobalOptions())
                .Then(x => x.ThenTheFollowingIsReturned(expected))
                .BDDfy();
        }

        private void GivenTheFollowingFileReRoute(FileReRoute fileReRoute)
        {
            _fileReRoute = fileReRoute;
        }

        private void GivenTheFollowingFileGlobalConfig(FileGlobalConfiguration fileGlobalConfig)
        {
            _fileGlobalConfig = fileGlobalConfig;
        }

        private void GivenTheFollowingInternalConfig(IInternalConfiguration config)
        {
            _internalConfig = config;
        }

        private void GivenTheFollowingGlobalOptions(FileRateLimitOptions options)
        {
            _fileRateLimitOptions = options;
        }

        private void GivenRateLimitingIsEnabled()
        {
            _enabled = true;
        }

        private void WhenICreate()
        {
            _result = _creator.Create(_fileReRoute, _fileGlobalConfig, _enabled);
        }

        private void WhenICreateWithInternalConfig()
        {
            _result = _creator.Create(_fileReRoute, _internalConfig, _enabled);
        }

        private void WhenICreateWithGlobalOptions()
        {
            _globalOptionsResult = _creator.Create(_fileRateLimitOptions);
        }

        private void ThenTheFollowingIsReturned(RateLimitOptions expected)
        {
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

        private void ThenTheFollowingIsReturned(RateLimitGlobalOptions expected)
        {
            _globalOptionsResult.ClientIdHeader.ShouldBe(expected.ClientIdHeader);
            _globalOptionsResult.DisableRateLimitHeaders.ShouldBe(expected.DisableRateLimitHeaders);
            _globalOptionsResult.HttpStatusCode.ShouldBe(expected.HttpStatusCode);
            _globalOptionsResult.QuotaExceededMessage.ShouldBe(expected.QuotaExceededMessage);
            _globalOptionsResult.RateLimitCounterPrefix.ShouldBe(expected.RateLimitCounterPrefix);
        }
    }
}
