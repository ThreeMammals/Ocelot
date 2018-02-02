using System.Collections.Generic;
using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.Provider;
using Ocelot.Configuration.Repository;
using Ocelot.Errors;
using Ocelot.Responses;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Configuration
{
    public class OcelotConfigurationProviderTests
    {
        private readonly IOcelotConfigurationProvider _ocelotConfigurationProvider;
        private readonly Mock<IOcelotConfigurationRepository> _configurationRepository;
        private Response<IOcelotConfiguration> _result;

        public OcelotConfigurationProviderTests()
        {
            _configurationRepository = new Mock<IOcelotConfigurationRepository>();
            _ocelotConfigurationProvider = new OcelotConfigurationProvider(_configurationRepository.Object);
        }

        [Fact]
        public void should_get_config()
        {
            var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();

            this.Given(x => x.GivenTheRepoReturns(new OkResponse<IOcelotConfiguration>(new OcelotConfiguration(new List<ReRoute>(), string.Empty, serviceProviderConfig, ""))))
                .When(x => x.WhenIGetTheConfig())
                .Then(x => x.TheFollowingIsReturned(new OkResponse<IOcelotConfiguration>(new OcelotConfiguration(new List<ReRoute>(), string.Empty, serviceProviderConfig, ""))))
                .BDDfy();
        }

        [Fact]
        public void should_return_error()
        {
            this.Given(x => x.GivenTheRepoReturns(new ErrorResponse<IOcelotConfiguration>(new List<Error>
                    {
                        new AnyError()
                    })))
              .When(x => x.WhenIGetTheConfig())
              .Then(x => x.TheFollowingIsReturned(
                    new ErrorResponse<IOcelotConfiguration>(new List<Error>
                    {
                        new AnyError()
                    })))
              .BDDfy();
        }

        private void GivenTheRepoReturns(Response<IOcelotConfiguration> config)
        {
            _configurationRepository
                .Setup(x => x.Get())
                .ReturnsAsync(config);
        }

        private void WhenIGetTheConfig()
        {
            _result = _ocelotConfigurationProvider.Get().Result;
        }

        private void TheFollowingIsReturned(Response<IOcelotConfiguration> expected)
        {
            _result.IsError.ShouldBe(expected.IsError);
        }

        class AnyError : Error
        {
            public AnyError() 
                : base("blamo", OcelotErrorCode.UnknownError)
            {
            }
        }
    }
}
