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
					ApiName = "Laura",
                    RequireHttps = true,
					AllowedScopes = new List<string> {"cheese"},
                    ApiSecret = "secret"
                }
            };

            var expected = new AuthenticationOptionsBuilder()
                    .WithProvider(fileReRoute.AuthenticationOptions?.Provider)
                    .WithProviderRootUrl(fileReRoute.AuthenticationOptions?.ProviderRootUrl)
                    .WithApiName(fileReRoute.AuthenticationOptions?.ApiName)
                    .WithRequireHttps(fileReRoute.AuthenticationOptions.RequireHttps)
                    .WithAllowedScopes(fileReRoute.AuthenticationOptions?.AllowedScopes)
                    .WithApiSecret(fileReRoute.AuthenticationOptions?.ApiSecret)
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
            _result.AllowedScopes.ShouldBe(expected.AllowedScopes);
            _result.Provider.ShouldBe(expected.Provider);
            _result.ProviderRootUrl.ShouldBe(expected.ProviderRootUrl);
            _result.RequireHttps.ShouldBe(expected.RequireHttps);
            _result.ApiName.ShouldBe(expected.ApiName);
            _result.ApiSecret.ShouldBe(expected.ApiSecret);
        }
    }
}