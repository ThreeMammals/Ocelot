using Microsoft.AspNetCore.Mvc;
using Ocelot.Administration;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.Configuration.Setter;
using Ocelot.Errors;
using Ocelot.Responses;

namespace Ocelot.UnitTests.Controllers;

public class FileConfigurationControllerTests : UnitTest
{
    private readonly FileConfigurationController _controller;
    private readonly Mock<IFileConfigurationRepository> _repo;
    private readonly Mock<IFileConfigurationSetter> _setter;

    public FileConfigurationControllerTests()
    {
        _repo = new Mock<IFileConfigurationRepository>();
        _setter = new Mock<IFileConfigurationSetter>();
        _controller = new FileConfigurationController(_repo.Object, _setter.Object);
    }

    [Fact]
    public async Task Should_get_file_configuration()
    {
        // Arrange
        var expected = new OkResponse<FileConfiguration>(new FileConfiguration());
        _repo.Setup(x => x.Get()).ReturnsAsync(expected);

        // Act
        var result = await _controller.Get();

        // Assert
        _repo.Verify(x => x.Get(), Times.Once);
    }

    [Fact]
    public async Task Should_return_error_when_cannot_get_config()
    {
        // Arrange
        var expected = new ErrorResponse<FileConfiguration>(It.IsAny<Error>());
        _repo.Setup(x => x.Get()).ReturnsAsync(expected);

        // Act
        var result = await _controller.Get();

        // Assert
        _repo.Verify(x => x.Get(), Times.Once);
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Should_post_file_configuration()
    {
        // Arrange
        var expected = new FileConfiguration();
        _setter.Setup(x => x.Set(It.IsAny<FileConfiguration>())).ReturnsAsync(new OkResponse());

        // Act
        var result = await _controller.Post(expected);

        // Assert
        _setter.Verify(x => x.Set(expected), Times.Once);
    }

    [Fact]
    public async Task Should_return_error_when_cannot_set_config()
    {
        // Arrange
        var expected = new FileConfiguration();
        _setter.Setup(x => x.Set(It.IsAny<FileConfiguration>())).ReturnsAsync(new ErrorResponse(new FakeError()));

        // Act
        var result = await _controller.Post(expected);

        // Assert
        _setter.Verify(x => x.Set(expected), Times.Once);
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Should_catch_exception_when_cannot_set_config()
    {
        // Arrange
        var expected = new FileConfiguration();
        _setter.Setup(x => x.Set(It.IsAny<FileConfiguration>()))
            .Throws(new Exception("Service failed"));

        // Act
        var result = await _controller.Post(expected);

        // Assert
        _setter.Verify(x => x.Set(expected), Times.Once);
        result.ShouldBeOfType<BadRequestObjectResult>();
        var actual = result as BadRequestObjectResult;
        Assert.StartsWith("Service failed:", actual.Value.ToString());
    }

    private class FakeError : Error
    {
        public FakeError() : base(string.Empty, OcelotErrorCode.CannotAddDataError, 404)
        {
        }
    }
}
