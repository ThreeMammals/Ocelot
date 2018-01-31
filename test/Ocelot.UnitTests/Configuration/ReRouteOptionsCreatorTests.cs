using System.Collections.Generic;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Configuration
{
    public class ReRouteOptionsCreatorTests
    {
        private ReRouteOptionsCreator _creator;
        private FileReRoute _reRoute;
        private ReRouteOptions _result;

        public ReRouteOptionsCreatorTests()
        {
            _creator = new ReRouteOptionsCreator();
        }
        
        [Fact]
        public void should_create_re_route_options()
        {
            var reRoute = new FileReRoute
            {
                RateLimitOptions = new FileRateLimitRule
                {
                    EnableRateLimiting = true
                },
                QoSOptions = new FileQoSOptions
                {
                    ExceptionsAllowedBeforeBreaking = 1,
                    TimeoutValue = 1
                },
                AuthenticationOptions = new FileAuthenticationOptions()
                {
                    AuthenticationProviderKey = "Test"
                },
                RouteClaimsRequirement = new Dictionary<string, string>()
                {
                    {"",""}
                },
                FileCacheOptions = new FileCacheOptions
                {
                    TtlSeconds = 1
                }
            };

            var expected = new ReRouteOptionsBuilder()
                .WithIsAuthenticated(true)
                .WithIsAuthorised(true)
                .WithIsCached(true)
                .WithIsQos(true)
                .WithRateLimiting(true)
                .Build();

            this.Given(x => x.GivenTheFollowing(reRoute))
                .When(x => x.WhenICreate())
                .Then(x => x.ThenTheFollowingIsReturned(expected))
                .BDDfy();
        }

        private void GivenTheFollowing(FileReRoute reRoute)
        {
            _reRoute = reRoute;
        }

        private void WhenICreate()
        {
            _result = _creator.Create(_reRoute);
        }

        private void ThenTheFollowingIsReturned(ReRouteOptions expected)
        {
            _result.IsAuthenticated.ShouldBe(expected.IsAuthenticated);
            _result.IsAuthorised.ShouldBe(expected.IsAuthorised);
            _result.IsQos.ShouldBe(expected.IsQos);
            _result.IsCached.ShouldBe(expected.IsCached);
            _result.EnableRateLimiting.ShouldBe(expected.EnableRateLimiting);
        }
    }
}