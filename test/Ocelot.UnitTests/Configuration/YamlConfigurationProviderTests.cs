using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Ocelot.Library.Configuration;
using Ocelot.Library.Configuration.Creator;
using Ocelot.Library.Configuration.Provider;
using Ocelot.Library.Configuration.Repository;
using Ocelot.Library.Configuration.Yaml;
using Ocelot.Library.Errors;
using Ocelot.Library.Responses;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Configuration
{
    public class YamlConfigurationProviderTests
    {
        private readonly IOcelotConfigurationProvider _ocelotConfigurationProvider;
        private readonly Mock<IOcelotConfigurationRepository> _configurationRepository;
        private readonly Mock<IOcelotConfigurationCreator> _creator;
        private Response<IOcelotConfiguration> _result;

        public YamlConfigurationProviderTests()
        {
            _creator = new Mock<IOcelotConfigurationCreator>();
            _configurationRepository = new Mock<IOcelotConfigurationRepository>();
            _ocelotConfigurationProvider = new YamlOcelotConfigurationProvider(_configurationRepository.Object, _creator.Object);
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
                .Returns(config);
        }

        private void GivenTheRepoReturns(Response<IOcelotConfiguration> config)
        {
            _configurationRepository
                .Setup(x => x.Get())
                .Returns(config);
        }

        private void WhenIGetTheConfig()
        {
            _result = _ocelotConfigurationProvider.Get();
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
