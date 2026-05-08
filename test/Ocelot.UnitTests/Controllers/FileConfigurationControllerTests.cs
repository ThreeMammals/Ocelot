using Microsoft.AspNetCore.Mvc;
using Ocelot.Administration;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.Errors;

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

    protected static CancellationToken CancelMe => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Should_get_file_configuration()
    {
        // Arrange
        var expected = new FileConfiguration();
        _repo.Setup(x => x.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.Get(CancelMe);

        // Assert
        _repo.Verify(x => x.GetAsync(It.IsAny<CancellationToken>()),
            Times.Once);
        result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Should_return_error_when_cannot_get_config()
    {
        // Arrange
        FileConfiguration expected = null;
        _repo.Setup(x => x.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.Get(CancelMe);

        // Assert
        _repo.Verify(x => x.GetAsync(It.IsAny<CancellationToken>()),
            Times.Once);
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Should_catch_exception_when_get_throws()
    {
        // Arrange
        _repo.Setup(x => x.GetAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Get failed"));

        // Act
        var result = await _controller.Get(CancelMe);

        // Assert
        result.ShouldBeOfType<BadRequestObjectResult>();
        var badRequest = result as BadRequestObjectResult;
        Assert.IsType<Exception>(badRequest.Value);
    }

    [Fact]
    public async Task Should_post_file_configuration()
    {
        // Arrange
        var expected = new FileConfiguration();

        // Act
        var result = await _controller.Post(expected, CancelMe);

        // Assert
        _setter.Verify(x => x.SetAsync(expected, It.IsAny<CancellationToken>()), Times.Once);
        result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Should_return_error_when_cannot_set_config()
    {
        // Arrange
        var expected = new FileConfiguration();
        _setter.Setup(x => x.SetAsync(It.IsAny<FileConfiguration>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("testMe"));

        // Act
        var result = await _controller.Post(expected, CancelMe);

        // Assert
        _setter.Verify(x => x.SetAsync(expected, It.IsAny<CancellationToken>()), Times.Once);
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Should_catch_exception_when_cannot_set_config()
    {
        // Arrange
        var expected = new FileConfiguration();
        _setter.Setup(x => x.SetAsync(It.IsAny<FileConfiguration>(), It.IsAny<CancellationToken>()))
            .Throws(new Exception("Service failed"));

        // Act
        var result = await _controller.Post(expected, CancelMe);

        // Assert
        _setter.Verify(x => x.SetAsync(expected, It.IsAny<CancellationToken>()), Times.Once);
        result.ShouldBeOfType<BadRequestObjectResult>();
        var actual = result as BadRequestObjectResult;
        Assert.StartsWith("System.Exception: Service failed", actual.Value.ToString());
    }

    private class FakeError : Error
    {
        public FakeError() : base(string.Empty, OcelotErrorCode.CannotAddDataError, 404)
        { }
    }
}
