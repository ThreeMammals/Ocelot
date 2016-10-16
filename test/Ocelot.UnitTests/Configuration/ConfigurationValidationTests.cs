using System.Collections.Generic;
using Ocelot.Library.Infrastructure.Configuration.Yaml;
using Ocelot.Library.Infrastructure.Responses;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Configuration
{
    public class ConfigurationValidationTests
    {
        private YamlConfiguration _yamlConfiguration;
        private readonly IConfigurationValidator _configurationValidator;
        private Response<ConfigurationValidationResult> _result;

        public ConfigurationValidationTests()
        {
            _configurationValidator = new ConfigurationValidator();
        }

        [Fact]
        public void configuration_is_valid_with_one_reroute()
        {
            this.Given(x => x.GivenAConfiguration(new YamlConfiguration()
            {
                ReRoutes = new List<YamlReRoute>
                {
                    new YamlReRoute
                    {
                        DownstreamTemplate = "http://www.bbc.co.uk",
                        UpstreamTemplate = "http://asdf.com"
                    }
                }
            }))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsValid())
                .BDDfy();
        }

        [Fact]
        public void configuration_is_valid_with_valid_authentication_provider()
        {
            this.Given(x => x.GivenAConfiguration(new YamlConfiguration()
            {
                ReRoutes = new List<YamlReRoute>
                {
                    new YamlReRoute
                    {
                        DownstreamTemplate = "http://www.bbc.co.uk",
                        UpstreamTemplate = "http://asdf.com",
                        AuthenticationOptions = new YamlAuthenticationOptions
                        {
                            Provider = "IdentityServer"
                        }
                    }
                }
            }))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsValid())
                .BDDfy();
        }

        [Fact]
        public void configuration_is_invalid_with_invalid_authentication_provider()
        {
            this.Given(x => x.GivenAConfiguration(new YamlConfiguration()
            {
                ReRoutes = new List<YamlReRoute>
                {
                    new YamlReRoute
                    {
                        DownstreamTemplate = "http://www.bbc.co.uk",
                        UpstreamTemplate = "http://asdf.com",
                        AuthenticationOptions = new YamlAuthenticationOptions
                        {
                            Provider = "BootyBootyBottyRockinEverywhere"
                        }
                    }
                }
            }))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorIs<UnsupportedAuthenticationProviderError>())
                .BDDfy();
        }

        [Fact]
        public void configuration_is_not_valid_with_duplicate_reroutes()
        {
            this.Given(x => x.GivenAConfiguration(new YamlConfiguration()
            {
                ReRoutes = new List<YamlReRoute>
                {
                    new YamlReRoute
                    {
                        DownstreamTemplate = "http://www.bbc.co.uk",
                        UpstreamTemplate = "http://asdf.com"
                    },
                    new YamlReRoute
                    {
                        DownstreamTemplate = "http://www.bbc.co.uk",
                        UpstreamTemplate = "http://lol.com"
                    }
                }
            }))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorIs<DownstreamTemplateAlreadyUsedError>())
                .BDDfy();
        }

        private void GivenAConfiguration(YamlConfiguration yamlConfiguration)
        {
            _yamlConfiguration = yamlConfiguration;
        }

        private void WhenIValidateTheConfiguration()
        {
            _result = _configurationValidator.IsValid(_yamlConfiguration);
        }

        private void ThenTheResultIsValid()
        {
            _result.Data.IsError.ShouldBeFalse();
        }

        private void ThenTheResultIsNotValid()
        {
            _result.Data.IsError.ShouldBeTrue();
        }

        private void ThenTheErrorIs<T>()
        {
            _result.Data.Errors[0].ShouldBeOfType<T>();
        }
    }
}
