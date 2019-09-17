using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Shouldly;
using System.Collections.Generic;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Configuration
{
    public class AuthenticationOptionsCreatorTests
    {
        private readonly AuthenticationOptionsCreator _authOptionsCreator;
        private FileReRoute _fileReRoute;
        private AuthenticationOptions _result;

        public AuthenticationOptionsCreatorTests()
        {
            _authOptionsCreator = new AuthenticationOptionsCreator();
        }

        [Fact]
        public void should_return_auth_options()
        {
            var fileReRoute = new FileReRoute()
            {
                AuthenticationOptions = new FileAuthenticationOptions
                {
                    AuthenticationProviderKey = "Test",
                    AllowedScopes = new List<string> { "cheese" },
                }
            };

            var expected = new AuthenticationOptionsBuilder()
                    .WithAllowedScopes(fileReRoute.AuthenticationOptions?.AllowedScopes)
                    .WithAuthenticationProviderKey("Test")
                    .Build();

            this.Given(x => x.GivenTheFollowing(fileReRoute))
                .When(x => x.WhenICreateTheAuthenticationOptions())
                .Then(x => x.ThenTheFollowingConfigIsReturned(expected))
                .BDDfy();
        }

        private void GivenTheFollowing(FileReRoute fileReRoute)
        {
            _fileReRoute = fileReRoute;
        }

        private void WhenICreateTheAuthenticationOptions()
        {
            _result = _authOptionsCreator.Create(_fileReRoute);
        }

        private void ThenTheFollowingConfigIsReturned(AuthenticationOptions expected)
        {
            _result.AllowedScopes.ShouldBe(expected.AllowedScopes);
            _result.AuthenticationProviderKey.ShouldBe(expected.AuthenticationProviderKey);
        }
    }
}
