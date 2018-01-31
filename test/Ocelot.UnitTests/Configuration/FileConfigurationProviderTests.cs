using System;
using System.Collections.Generic;
using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.Responses;
using Shouldly;
using TestStack.BDDfy;
using Xunit;
using Newtonsoft.Json;
using System.IO;
using Ocelot.Configuration.Provider;
using Ocelot.Configuration.Repository;

namespace Ocelot.UnitTests.Configuration
{
    public class FileConfigurationProviderTests
    {
        private readonly IFileConfigurationProvider _provider;
        private Mock<IFileConfigurationRepository> _repo;
        private FileConfiguration _result;
        private FileConfiguration _fileConfiguration;

        public FileConfigurationProviderTests()
        {
            _repo = new Mock<IFileConfigurationRepository>();
            _provider = new FileConfigurationProvider(_repo.Object);
        }

        [Fact]
        public void should_return_file_configuration()
        {
            var config = new FileConfiguration();

            this.Given(x => x.GivenTheConfigurationIs(config))
                .When(x => x.WhenIGetTheReRoutes())
                .Then(x => x.ThenTheRepoIsCalledCorrectly())
                .BDDfy();
        }

      

        private void GivenTheConfigurationIs(FileConfiguration fileConfiguration)
        {
            _fileConfiguration = fileConfiguration;
            _repo
                .Setup(x => x.Get())
                .ReturnsAsync(new OkResponse<FileConfiguration>(fileConfiguration));
        }

        private void WhenIGetTheReRoutes()
        {
            _result = _provider.Get().Result.Data;
        }

        private void ThenTheRepoIsCalledCorrectly()
        {
            _repo
                .Verify(x => x.Get(), Times.Once);
        }
    }
}