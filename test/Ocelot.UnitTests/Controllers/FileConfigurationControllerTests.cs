using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.Controllers;
using Ocelot.Responses;
using Ocelot.Services;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Controllers
{
    public class FileConfigurationControllerTests
    {
        private FileConfigurationController _controller;
        private Mock<IGetFileConfiguration> _getFileConfig;
        private IActionResult _result;

        public FileConfigurationControllerTests()
        {
            _getFileConfig = new Mock<IGetFileConfiguration>();
            _controller = new FileConfigurationController(_getFileConfig.Object);
        }
        
        [Fact]
        public void should_return_file_configuration()
        {
            var expected = new OkResponse<FileConfiguration>(new FileConfiguration());

            this.Given(x => x.GivenTheGetConfigurationReturns(expected))
                .When(x => x.WhenIGetTheFileConfiguration())
                .Then(x => x.ThenTheFileConfigurationIsReturned(expected.Data))
                .BDDfy();
        }

        private void GivenTheGetConfigurationReturns(Response<FileConfiguration> fileConfiguration)
        {
            _getFileConfig
                .Setup(x => x.Invoke())
                .Returns(fileConfiguration);
        }

        private void WhenIGetTheFileConfiguration()
        {
            _result = _controller.Get();
        }

        private void ThenTheFileConfigurationIsReturned(FileConfiguration expected)
        {
        }
    }
}