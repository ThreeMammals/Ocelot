using System.Collections.Generic;
using Moq;
using Ocelot.Configuration;
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
    public class FileConfigurationProviderTests
    {
        private readonly IOcelotConfigurationProvider _ocelotConfigurationProvider;
        private readonly Mock<IOcelotConfigurationRepository> _configurationRepository;
        private readonly Mock<IOcelotConfigurationCreator> _creator;
        private Response<IOcelotConfiguration> _result;

        public FileConfigurationProviderTests()
        {
            _creator = new Mock<IOcelotConfigurationCreator>();
            _configurationRepository = new Mock<IOcelotConfigurationRepository>();
            _ocelotConfigurationProvider = new OcelotConfigurationProvider(_configurationRepository.Object, _creator.Object);
        }

        [Fact]
        public void should_get_config()
        {
            this.Given(x => x.GivenTheRepoReturns(new OkResponse<IOcelotConfiguration>(new OcelotConfiguration(new List<ReRoute>()))))
                .When(x => x.WhenIGetTheConfig())
                .Then(x => x.TheFollowingIsReturned(new OkResponse<IOcelotConfiguration>(new OcelotConfiguration(new List<ReRoute>()))))
                .BDDfy();
        }

        [Fact]
        public void should_create_config_if_it_doesnt_exist()
        {
            this.Given(x => x.GivenTheRepoReturns(new OkResponse<IOcelotConfiguration>(null)))
                .And(x => x.GivenTheCreatorReturns(new OkResponse<IOcelotConfiguration>(new OcelotConfiguration(new List<ReRoute>()))))
                .When(x => x.WhenIGetTheConfig())
                .Then(x => x.TheFollowingIsReturned(new OkResponse<IOcelotConfiguration>(new OcelotConfiguration(new List<ReRoute>()))))
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

        [Fact]
        public void should_return_error_if_creator_errors()
        {
            this.Given(x => x.GivenTheRepoReturns(new OkResponse<IOcelotConfiguration>(null)))
                .And(x => x.GivenTheCreatorReturns(new ErrorResponse<IOcelotConfiguration>(new List<Error>
                    {
                        new AnyError()
                    })))
                .When(x => x.WhenIGetTheConfig())
                .Then(x => x.TheFollowingIsReturned(new ErrorResponse<IOcelotConfiguration>(new List<Error>
                    {
                        new AnyError()
                    })))
                .BDDfy();
        }

        private void GivenTheCreatorReturns(Response<IOcelotConfiguration> config)
        {
            _creator
                .Setup(x => x.Create())
                .ReturnsAsync(config);
        }

        private void GivenTheRepoReturns(Response<IOcelotConfiguration> config)
        {
            _configurationRepository
                .Setup(x => x.Get())
                .Returns(config);
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
