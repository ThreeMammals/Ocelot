namespace Ocelot.UnitTests.Configuration
{
    using Ocelot.Configuration;
    using Ocelot.Configuration.Builder;
    using Ocelot.Configuration.Creator;
    using Ocelot.Configuration.File;
    using Shouldly;
    using System.Collections.Generic;
    using TestStack.BDDfy;
    using Xunit;

    public class ReRouteOptionsCreatorTests
    {
        private readonly ReRouteOptionsCreator _creator;
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
                },
                ServiceName = "west"
            };

            var expected = new ReRouteOptionsBuilder()
                .WithIsAuthenticated(true)
                .WithIsAuthorised(true)
                .WithIsCached(true)
                .WithRateLimiting(true)
                .WithUseServiceDiscovery(true)
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
            _result.IsCached.ShouldBe(expected.IsCached);
            _result.EnableRateLimiting.ShouldBe(expected.EnableRateLimiting);
            _result.UseServiceDiscovery.ShouldBe(expected.UseServiceDiscovery);
        }
    }
}
