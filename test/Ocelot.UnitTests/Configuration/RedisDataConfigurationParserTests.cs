using Ocelot.Configuration.File;
using Ocelot.Configuration.Parser;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Configuration
{
    public class RedisDataConfigurationParserTests
    {
        private IRedisDataConfigurationParser _parser;
        private List<HashEntry> _hashEntries;
        private FileReRoute _result;

        public RedisDataConfigurationParserTests()
        {
            _parser = new RedisDataConfigurationParser();
        }
        
        [Fact]
        public void should_parse_reroute_configuration()
        {
            var hashEntries = new List<HashEntry>()
            {
                new HashEntry(ConfigurationKeys.RateLimit.CLIENT_WHITE_LIST, "  client1, client2  "),
                new HashEntry(ConfigurationKeys.RateLimit.ENABLE_RATE_LIMITING, true),
                new HashEntry(ConfigurationKeys.RateLimit.LIMIT, "30"),
                new HashEntry(ConfigurationKeys.RateLimit.PERIOD, "10s"),
                new HashEntry(ConfigurationKeys.RateLimit.PERIOD_TIME_SPAN, "60.5")
            };

            var expected = new FileReRoute
            {
                RateLimitOptions = new FileRateLimitRule
                {
                    ClientWhitelist = new List<string>() { "client1", "client2" },
                    EnableRateLimiting = true,
                    Limit = 30,
                    Period = "10s",
                    PeriodTimespan = 60.5
                }
            };

            this.Given(x => x.GivenHashEntries(hashEntries))
                .When(x => x.WhenIParse())
                .Then(x => x.ThenTheReRouteIs(expected))
                .BDDfy();
        }
        
        private void GivenHashEntries(List<HashEntry> entries)
        {
            _hashEntries = entries;
        }

        private void WhenIParse()
        {
            _result = _parser.Parse(_hashEntries);
        }

        private void ThenTheReRouteIs(FileReRoute expected)
        {
            _result.RateLimitOptions.ClientWhitelist.ShouldBe(expected.RateLimitOptions.ClientWhitelist);
            _result.RateLimitOptions.EnableRateLimiting.ShouldBe(expected.RateLimitOptions.EnableRateLimiting);
            _result.RateLimitOptions.Limit.ShouldBe(expected.RateLimitOptions.Limit);
            _result.RateLimitOptions.Period.ShouldBe(expected.RateLimitOptions.Period);
            _result.RateLimitOptions.PeriodTimespan.ShouldBe(expected.RateLimitOptions.PeriodTimespan);
        }
    }
}
