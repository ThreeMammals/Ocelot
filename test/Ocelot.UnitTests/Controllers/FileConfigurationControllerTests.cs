using Microsoft.AspNetCore.Mvc;
using Moq;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Setter;
using Ocelot.Controllers;
using Ocelot.Errors;
using Ocelot.Responses;
using TestStack.BDDfy;
using Xunit;
using Shouldly;
using Ocelot.Configuration.Provider;

namespace Ocelot.UnitTests.Controllers
{
    public class FileConfigurationControllerTests
    {
        private FileConfigurationController _controller;
        private Mock<IFileConfigurationProvider> _configGetter;
        private Mock<IFileConfigurationSetter> _configSetter;
        private IActionResult _result;
        private FileConfiguration _fileConfiguration;

        public FileConfigurationControllerTests()
        {
            _configGetter = new Mock<IFileConfigurationProvider>();
            _configSetter = new Mock<IFileConfigurationSetter>();
            _controller = new FileConfigurationController(_configGetter.Object, _configSetter.Object);
        }
        
        [Fact]
        public void should_get_file_configuration()
        {
            var expected = new OkResponse<FileConfiguration>(new FileConfiguration());

            this.Given(x => x.GivenTheGetConfigurationReturns(expected))
                .When(x => x.WhenIGetTheFileConfiguration())
                .Then(x => x.TheTheGetFileConfigurationIsCalledCorrectly())
                .BDDfy();
        }

        [Fact]
        public void should_return_error_when_cannot_get_config()
        {
            var expected = new ErrorResponse<FileConfiguration>(It.IsAny<Error>());

             this.Given(x => x.GivenTheGetConfigurationReturns(expected))
                .When(x => x.WhenIGetTheFileConfiguration())
                .Then(x => x.TheTheGetFileConfigurationIsCalledCorrectly())
                .And(x => x.ThenTheResponseIs<BadRequestObjectResult>())
                .BDDfy();
        } 

        [Fact]
        public void should_post_file_configuration()
        {
            var expected = new FileConfiguration();

            this.Given(x => GivenTheFileConfiguration(expected))
                .And(x => GivenTheConfigSetterReturnsAnError(new OkResponse()))
                .When(x => WhenIPostTheFileConfiguration())
                .Then(x => x.ThenTheConfigrationSetterIsCalledCorrectly())
                .BDDfy();
        }

        [Fact]
        public void should_return_error_when_cannot_set_config()
        {
            var expected = new FileConfiguration();

            this.Given(x => GivenTheFileConfiguration(expected))
                .And(x => GivenTheConfigSetterReturnsAnError(new ErrorResponse(new FakeError())))
                .When(x => WhenIPostTheFileConfiguration())
                .Then(x => x.ThenTheConfigrationSetterIsCalledCorrectly())
                .And(x => ThenTheResponseIs<BadRequestObjectResult>())
                .BDDfy();
        }

        private void GivenTheConfigSetterReturnsAnError(Response response)
        {
            _configSetter
                .Setup(x => x.Set(It.IsAny<FileConfiguration>()))
                .ReturnsAsync(response);
        }

        private void ThenTheConfigrationSetterIsCalledCorrectly()
        {
            _configSetter
                .Verify(x => x.Set(_fileConfiguration), Times.Once);
        }

        private void WhenIPostTheFileConfiguration()
        {
            _result = _controller.Post(_fileConfiguration).Result;
        }

        private void GivenTheFileConfiguration(FileConfiguration fileConfiguration)
        {
            _fileConfiguration = fileConfiguration;
        }

        private void ThenTheResponseIs<T>()
        {
           _result.ShouldBeOfType<T>();
        }

        private void GivenTheGetConfigurationReturns(Response<FileConfiguration> fileConfiguration)
        {
            _configGetter
                .Setup(x => x.Get())
                .Returns(fileConfiguration);
        }

        private void WhenIGetTheFileConfiguration()
        {
            _result = _controller.Get();
        }

        private void TheTheGetFileConfigurationIsCalledCorrectly()
        {
               _configGetter
                .Verify(x => x.Get(), Times.Once);
        }

        class FakeError : Error
        {
            public FakeError() : base(string.Empty, OcelotErrorCode.CannotAddDataError)
            {
            }
        }
    }
}