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
                    IdentityServerConfig = new FileIdentityServerConfig()
                    {
                        ProviderRootUrl = "http://www.bbc.co.uk/",
                        ApiName = "Laura",
                        RequireHttps = true,
                        ApiSecret = "secret"
                    },
                    AllowedScopes = new List<string> { "cheese" },
                    
                }
            };

            var authenticationConfig = new IdentityServerConfigBuilder()
                .WithProviderRootUrl(fileReRoute.AuthenticationOptions?.IdentityServerConfig?.ProviderRootUrl)
                .WithApiName(fileReRoute.AuthenticationOptions?.IdentityServerConfig?.ApiName)
                .WithRequireHttps(fileReRoute.AuthenticationOptions.IdentityServerConfig.RequireHttps)
                .WithApiSecret(fileReRoute.AuthenticationOptions?.IdentityServerConfig?.ApiSecret)
                .Build();

            var expected = new AuthenticationOptionsBuilder()
                    .WithProvider(fileReRoute.AuthenticationOptions?.Provider)
                    .WithAllowedScopes(fileReRoute.AuthenticationOptions?.AllowedScopes)
                    .WithConfig(authenticationConfig)
                    .Build();

            this.Given(x => x.GivenTheFollowing(fileReRoute))
                .When(x => x.WhenICreateTheAuthenticationOptions())
                .Then(x => x.ThenTheFollowingIdentityServerConfigIsReturned(expected))
                .BDDfy();
        }

        [Fact]
        public void should_return_Jwt_auth_options()
        {
            var fileReRoute = new FileReRoute()
            {
                AuthenticationOptions = new FileAuthenticationOptions
                {
                    Provider = "Jwt",
                    JwtConfig = new FileJwtConfig()
                    {
                        Audience  = "Audience",
                        Authority = "Authority"
                    },
                    AllowedScopes = new List<string> { "cheese" }
                }
            };

            var authenticationConfig = new JwtConfigBuilder()
                .WithAudience(fileReRoute.AuthenticationOptions?.JwtConfig?.Audience)
                .WithAuthority(fileReRoute.AuthenticationOptions?.JwtConfig?.Authority)
                .Build();

            var expected = new AuthenticationOptionsBuilder()
                .WithProvider(fileReRoute.AuthenticationOptions?.Provider)
                .WithAllowedScopes(fileReRoute.AuthenticationOptions?.AllowedScopes)
                .WithConfig(authenticationConfig)
                .Build();

            this.Given(x => x.GivenTheFollowing(fileReRoute))
                .When(x => x.WhenICreateTheAuthenticationOptions())
                .Then(x => x.ThenTheFollowingJwtConfigIsReturned(expected))
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

        private void ThenTheFollowingJwtConfigIsReturned(AuthenticationOptions expected)
        {
            _result.AllowedScopes.ShouldBe(expected.AllowedScopes);
            _result.Provider.ShouldBe(expected.Provider);

            var _resultSettings = _result.Config as JwtConfig;
            var expectedSettngs = expected.Config as JwtConfig;

            _resultSettings.Audience.ShouldBe(expectedSettngs.Audience);
            _resultSettings.Authority.ShouldBe(expectedSettngs.Authority);

        }

        private void ThenTheFollowingIdentityServerConfigIsReturned(AuthenticationOptions expected)
        {
            _result.AllowedScopes.ShouldBe(expected.AllowedScopes);
            _result.Provider.ShouldBe(expected.Provider);

            var _resultSettings = _result.Config as IdentityServerConfig;
            var expectedSettngs = expected.Config as IdentityServerConfig;

            _resultSettings.ProviderRootUrl.ShouldBe(expectedSettngs.ProviderRootUrl);
            _resultSettings.RequireHttps.ShouldBe(expectedSettngs.RequireHttps);
            _resultSettings.ApiName.ShouldBe(expectedSettngs.ApiName);
            _resultSettings.ApiSecret.ShouldBe(expectedSettngs.ApiSecret);
        }
    }
}