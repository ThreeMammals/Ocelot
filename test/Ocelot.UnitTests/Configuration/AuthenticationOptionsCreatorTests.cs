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
    public class AuthenticationOptionsCreatorTests
    {
        private readonly AuthenticationOptionsCreator _authOptionsCreator;

        private FileRoute _fileRoute;

        private AuthenticationOptions _result;

        public AuthenticationOptionsCreatorTests()
        {
            _authOptionsCreator = new AuthenticationOptionsCreator();
        }

        [InlineData(false)]
        [InlineData(true)]
        [Theory]
        public void should_return_auth_options(bool isAuthenticationProviderKeys)
        {
            string authenticationProviderKey = !isAuthenticationProviderKeys ? "Test" : null;
            List<string> authenticationProviderKeys = isAuthenticationProviderKeys ? new List<string> { "Test #1", "Test #2" } : null;
            var fileRoute = new FileRoute()
            {
                AuthenticationOptions = new FileAuthenticationOptions
                {
                    AllowedScopes = new List<string> { "cheese" },
                    AuthenticationProviderKey = authenticationProviderKey,
                    AuthenticationProviderKeys = authenticationProviderKeys
                }
            };

            var expected = new AuthenticationOptionsBuilder()
                    .WithAllowedScopes(fileRoute.AuthenticationOptions?.AllowedScopes)
                    .WithAuthenticationProviderKey(authenticationProviderKey)
                    .WithAuthenticationProviderKeys(authenticationProviderKeys)
                    .Build();

            this.Given(x => x.GivenTheFollowing(fileRoute))
                .When(x => x.WhenICreateTheAuthenticationOptions())
                .Then(x => x.ThenTheFollowingConfigIsReturned(expected))
                .BDDfy();
        }

        private void GivenTheFollowing(FileRoute fileRoute)
        {
            _fileRoute = fileRoute;
        }

        private void WhenICreateTheAuthenticationOptions()
        {
            _result = _authOptionsCreator.Create(_fileRoute);
        }

        private void ThenTheFollowingConfigIsReturned(AuthenticationOptions expected)
        {
            _result.AllowedScopes.ShouldBe(expected.AllowedScopes);
            _result.AuthenticationProviderKey.ShouldBe(expected.AuthenticationProviderKey);
            _result.AuthenticationProviderKeys.ShouldBe(expected.AuthenticationProviderKeys);
        }
    }
}
