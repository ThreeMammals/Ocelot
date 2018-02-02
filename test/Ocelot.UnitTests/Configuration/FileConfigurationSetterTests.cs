using System;
using System.Collections.Generic;
using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.Configuration.Setter;
using Ocelot.Errors;
using Ocelot.Responses;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Configuration
{
    public class FileConfigurationSetterTests
    {
        private FileConfiguration _fileConfiguration;
        private FileConfigurationSetter _configSetter;
        private Mock<IOcelotConfigurationRepository> _configRepo;
        private Mock<IOcelotConfigurationCreator> _configCreator;
        private Response<IOcelotConfiguration> _configuration;
        private object _result; 
        private Mock<IFileConfigurationRepository> _repo;

        public FileConfigurationSetterTests()
        {
            _repo = new Mock<IFileConfigurationRepository>();
            _configRepo = new Mock<IOcelotConfigurationRepository>();
            _configCreator = new Mock<IOcelotConfigurationCreator>();
            _configSetter = new FileConfigurationSetter(_configRepo.Object, _configCreator.Object, _repo.Object);
        }

        [Fact]
        public void should_set_configuration()
        {
            var fileConfig = new FileConfiguration();
            var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
            var config = new OcelotConfiguration(new List<ReRoute>(), string.Empty, serviceProviderConfig, "asdf");

            this.Given(x => GivenTheFollowingConfiguration(fileConfig))
                .And(x => GivenTheRepoReturns(new OkResponse()))
                .And(x => GivenTheCreatorReturns(new OkResponse<IOcelotConfiguration>(config)))
                .When(x => WhenISetTheConfiguration())
                .Then(x => ThenTheConfigurationRepositoryIsCalledCorrectly())
                .BDDfy();
        }


        [Fact]
        public void should_return_error_if_unable_to_set_file_configuration()
        {
            var fileConfig = new FileConfiguration();

            this.Given(x => GivenTheFollowingConfiguration(fileConfig))
                .And(x => GivenTheRepoReturns(new ErrorResponse(It.IsAny<Error>())))
                .When(x => WhenISetTheConfiguration())
                .And(x => ThenAnErrorResponseIsReturned())
                .BDDfy();
        }

        [Fact]
        public void should_return_error_if_unable_to_set_ocelot_configuration()
        {
            var fileConfig = new FileConfiguration();

            this.Given(x => GivenTheFollowingConfiguration(fileConfig))
                .And(x => GivenTheRepoReturns(new OkResponse()))
                .And(x => GivenTheCreatorReturns(new ErrorResponse<IOcelotConfiguration>(It.IsAny<Error>())))
                .When(x => WhenISetTheConfiguration())
                .And(x => ThenAnErrorResponseIsReturned())
                .BDDfy();
        }

        private void GivenTheRepoReturns(Response response)
        {
            _repo
                .Setup(x => x.Set(It.IsAny<FileConfiguration>()))
                .ReturnsAsync(response);
        }

        private void ThenAnErrorResponseIsReturned()
        {
            _result.ShouldBeOfType<ErrorResponse>();
        }

        private void GivenTheCreatorReturns(Response<IOcelotConfiguration> configuration)
        {
            _configuration = configuration;
            _configCreator
                .Setup(x => x.Create(_fileConfiguration))
                .ReturnsAsync(_configuration);
        }

        private void GivenTheFollowingConfiguration(FileConfiguration fileConfiguration)
        {
            _fileConfiguration = fileConfiguration;
        }

        private void WhenISetTheConfiguration()
        {
            _result = _configSetter.Set(_fileConfiguration).Result;
        }

        private void ThenTheConfigurationRepositoryIsCalledCorrectly()
        {
            _configRepo
                .Verify(x => x.AddOrReplace(_configuration.Data), Times.Once);
        }
    }
}