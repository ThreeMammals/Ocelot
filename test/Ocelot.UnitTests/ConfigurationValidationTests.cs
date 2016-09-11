using System.Collections.Generic;
using Ocelot.Library.Infrastructure.Configuration;
using Ocelot.Library.Infrastructure.Responses;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests
{
    public class ConfigurationValidationTests
    {
        private Configuration _configuration;
        private readonly IConfigurationValidator _configurationValidator;
        private Response<ConfigurationValidationResult> _result;

        public ConfigurationValidationTests()
        {
            _configurationValidator = new ConfigurationValidator();
        }

        [Fact]
        public void configuration_is_valid_with_one_reroute()
        {
            this.Given(x => x.GivenAConfiguration(new Configuration()
            {
                ReRoutes = new List<ReRoute>
                {
                    new ReRoute
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
        public void configuration_is_not_valid_with_duplicate_reroutes()
        {
            this.Given(x => x.GivenAConfiguration(new Configuration()
            {
                ReRoutes = new List<ReRoute>
                {
                    new ReRoute
                    {
                        DownstreamTemplate = "http://www.bbc.co.uk",
                        UpstreamTemplate = "http://asdf.com"
                    },
                    new ReRoute
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

        private void GivenAConfiguration(Configuration configuration)
        {
            _configuration = configuration;
        }

        private void WhenIValidateTheConfiguration()
        {
            _result = _configurationValidator.IsValid(_configuration);
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
