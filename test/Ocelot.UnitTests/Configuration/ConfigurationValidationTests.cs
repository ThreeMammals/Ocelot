using System.Collections.Generic;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Validator;
using Ocelot.Responses;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Configuration
{
    public class ConfigurationValidationTests
    {
        private FileConfiguration _fileConfiguration;
        private readonly IConfigurationValidator _configurationValidator;
        private Response<ConfigurationValidationResult> _result;

        public ConfigurationValidationTests()
        {
            _configurationValidator = new FileConfigurationValidator();
        }

        [Fact]
        public void configuration_is_invalid_if_scheme_in_downstream_template()
        {
                this.Given(x => x.GivenAConfiguration(new FileConfiguration()
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamTemplate = "http://www.bbc.co.uk/api/products/{productId}",
                        UpstreamTemplate = "http://asdf.com"
                    }
                }
            }))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .BDDfy();
        }

           [Fact]
        public void configuration_is_invalid_if_host_in_downstream_template()
        {
                this.Given(x => x.GivenAConfiguration(new FileConfiguration()
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamTemplate = "www.bbc.co.uk/api/products/{productId}",
                        UpstreamTemplate = "http://asdf.com"
                    }
                }
            }))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .BDDfy();
        }

        [Fact]
        public void configuration_is_valid_with_one_reroute()
        {
            this.Given(x => x.GivenAConfiguration(new FileConfiguration()
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamTemplate = "/api/products/",
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
            this.Given(x => x.GivenAConfiguration(new FileConfiguration()
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamTemplate = "/api/products/",
                        UpstreamTemplate = "http://asdf.com",
                        AuthenticationOptions = new FileAuthenticationOptions
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
            this.Given(x => x.GivenAConfiguration(new FileConfiguration()
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamTemplate = "/api/products/",
                        UpstreamTemplate = "http://asdf.com",
                        AuthenticationOptions = new FileAuthenticationOptions
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
            this.Given(x => x.GivenAConfiguration(new FileConfiguration()
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamTemplate = "/api/products/",
                        UpstreamTemplate = "http://asdf.com"
                    },
                    new FileReRoute
                    {
                        DownstreamTemplate = "http://www.bbc.co.uk",
                        UpstreamTemplate = "http://asdf.com"
                    }
                }
            }))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorIs<DownstreamTemplateAlreadyUsedError>())
                .BDDfy();
        }

        private void GivenAConfiguration(FileConfiguration fileConfiguration)
        {
            _fileConfiguration = fileConfiguration;
        }

        private void WhenIValidateTheConfiguration()
        {
            _result = _configurationValidator.IsValid(_fileConfiguration);
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
