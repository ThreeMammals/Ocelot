using System;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Setter;
using Ocelot.Errors;
using Ocelot.Responses;
using TestStack.BDDfy;
using Xunit;
using Shouldly;
using Ocelot.Configuration.Provider;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Raft;
using Rafty.Concensus;
using Newtonsoft.Json;
using Rafty.FiniteStateMachine;
using Ocelot.Configuration;

namespace Ocelot.UnitTests.Controllers
{
    public class FileConfigurationControllerTests
    {
        private FileConfigurationController _controller;
        private Mock<IFileConfigurationProvider> _configGetter;
        private Mock<IFileConfigurationSetter> _configSetter;
        private IActionResult _result;
        private FileConfiguration _fileConfiguration;
        private Mock<IServiceProvider> _provider;
        private Mock<INode> _node;

        public FileConfigurationControllerTests()
        {
            _provider = new Mock<IServiceProvider>();
            _configGetter = new Mock<IFileConfigurationProvider>();
            _configSetter = new Mock<IFileConfigurationSetter>();
            _controller = new FileConfigurationController(_configGetter.Object, _configSetter.Object, _provider.Object);
        }
        
        [Fact]
        public void should_get_file_configuration()
        {
            var expected = new Responses.OkResponse<FileConfiguration>(new FileConfiguration());

            this.Given(x => x.GivenTheGetConfigurationReturns(expected))
                .When(x => x.WhenIGetTheFileConfiguration())
                .Then(x => x.TheTheGetFileConfigurationIsCalledCorrectly())
                .BDDfy();
        }

        [Fact]
        public void should_return_error_when_cannot_get_config()
        {
            var expected = new Responses.ErrorResponse<FileConfiguration>(It.IsAny<Error>());

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
                .And(x => GivenTheConfigSetterReturns(new OkResponse()))
                .When(x => WhenIPostTheFileConfiguration())
                .Then(x => x.ThenTheConfigrationSetterIsCalledCorrectly())
                .BDDfy();
        }

        [Fact]
        public void should_post_file_configuration_using_raft_node()
        {
            var expected = new FileConfiguration();

            this.Given(x => GivenTheFileConfiguration(expected))
                .And(x => GivenARaftNodeIsRegistered())
                .And(x => GivenTheNodeReturnsOK())
                .And(x => GivenTheConfigSetterReturns(new OkResponse()))
                .When(x => WhenIPostTheFileConfiguration())
                .Then(x => x.ThenTheNodeIsCalledCorrectly())
                .BDDfy();
        }

        [Fact]
        public void should_return_error_when_cannot_set_config_using_raft_node()
        {
            var expected = new FileConfiguration();

            this.Given(x => GivenTheFileConfiguration(expected))
                .And(x => GivenARaftNodeIsRegistered())
                .And(x => GivenTheNodeReturnsError())
                .When(x => WhenIPostTheFileConfiguration())
                .Then(x => ThenTheResponseIs<BadRequestObjectResult>())
                .BDDfy();
        }

        [Fact]
        public void should_return_error_when_cannot_set_config()
        {
            var expected = new FileConfiguration();

            this.Given(x => GivenTheFileConfiguration(expected))
                .And(x => GivenTheConfigSetterReturns(new ErrorResponse(new FakeError())))
                .When(x => WhenIPostTheFileConfiguration())
                .Then(x => x.ThenTheConfigrationSetterIsCalledCorrectly())
                .And(x => ThenTheResponseIs<BadRequestObjectResult>())
                .BDDfy();
        }


        private void ThenTheNodeIsCalledCorrectly()
        {
            _node.Verify(x => x.Accept(It.IsAny<UpdateFileConfiguration>()), Times.Once);
        }

        private void GivenARaftNodeIsRegistered()
        {
            _node = new Mock<INode>();
            _provider
                .Setup(x => x.GetService(typeof(INode)))
                .Returns(_node.Object);
        }

        private void GivenTheNodeReturnsOK()
        {
            _node
                .Setup(x => x.Accept(It.IsAny<UpdateFileConfiguration>()))
                .Returns(new Rafty.Concensus.OkResponse<UpdateFileConfiguration>(new UpdateFileConfiguration(new FileConfiguration())));
        }

        private void GivenTheNodeReturnsError()
        {
            _node
                .Setup(x => x.Accept(It.IsAny<UpdateFileConfiguration>()))
                .Returns(new Rafty.Concensus.ErrorResponse<UpdateFileConfiguration>("error", new UpdateFileConfiguration(new FileConfiguration())));
        }

        private void GivenTheConfigSetterReturns(Response response)
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

        private void GivenTheGetConfigurationReturns(Ocelot.Responses.Response<FileConfiguration> fileConfiguration)
        {
            _configGetter
                .Setup(x => x.Get())
                .ReturnsAsync(fileConfiguration);
        }

        private void WhenIGetTheFileConfiguration()
        {
            _result = _controller.Get().Result;
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
