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
        private AuthenticationOptionsCreator _authOptionsCreator;
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
                    Provider = "Geoff",
                    ProviderRootUrl = "http://www.bbc.co.uk/",
                    ScopeName = "Laura",
                    RequireHttps = true,
                    AdditionalScopes = new List<string> {"cheese"},
                    ScopeSecret = "secret"
                }
            };

            var expected = new AuthenticationOptionsBuilder()
                    .WithProvider(fileReRoute.AuthenticationOptions?.Provider)
                    .WithProviderRootUrl(fileReRoute.AuthenticationOptions?.ProviderRootUrl)
                    .WithScopeName(fileReRoute.AuthenticationOptions?.ScopeName)
                    .WithRequireHttps(fileReRoute.AuthenticationOptions.RequireHttps)
                    .WithAdditionalScopes(fileReRoute.AuthenticationOptions?.AdditionalScopes)
                    .WithScopeSecret(fileReRoute.AuthenticationOptions?.ScopeSecret)
                    .Build();

            this.Given(x => x.GivenTheFollowing(fileReRoute))
                .When(x => x.WhenICreateTheAuthenticationOptions())
                .Then(x => x.ThenTheFollowingIsReturned(expected))
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

        private void ThenTheFollowingIsReturned(AuthenticationOptions expected)
        {
            _result.AdditionalScopes.ShouldBe(expected.AdditionalScopes);
            _result.Provider.ShouldBe(expected.Provider);
            _result.ProviderRootUrl.ShouldBe(expected.ProviderRootUrl);
            _result.RequireHttps.ShouldBe(expected.RequireHttps);
            _result.ScopeName.ShouldBe(expected.ScopeName);
            _result.ScopeSecret.ShouldBe(expected.ScopeSecret);
        }
    }
}