using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Repository;
using Ocelot.Logging;
using Ocelot.Middleware;

namespace Ocelot.UnitTests.Middleware;

public class ConfigurationMiddlewareTests : UnitTest
{
    private readonly Mock<IInternalConfigurationRepository> _configRepo;
    private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
    private readonly Mock<IOcelotLogger> _logger;
    private readonly DefaultHttpContext _httpContext;
    private bool _nextCalled;
    private readonly ConfigurationMiddleware _middleware;

    public ConfigurationMiddlewareTests()
    {
        _configRepo = new Mock<IInternalConfigurationRepository>();
        _loggerFactory = new Mock<IOcelotLoggerFactory>();
        _logger = new Mock<IOcelotLogger>();
        _loggerFactory.Setup(x => x.CreateLogger<ConfigurationMiddleware>()).Returns(_logger.Object);
        _httpContext = new DefaultHttpContext();
        _nextCalled = false;
        _middleware = new ConfigurationMiddleware(Next, _loggerFactory.Object, _configRepo.Object);
    }

    private Task Next(HttpContext ctx)
    {
        _nextCalled = true;
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Invoke_ShouldCallNext_WhenConfigIsNotNull()
    {
        // Arrange
        var config = new Mock<IInternalConfiguration>().Object;
        _configRepo.Setup(x => x.Get()).Returns(config);

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        Assert.True(_nextCalled);
        _configRepo.Verify(x => x.Get(), Times.Once);
        var storedConfig = _httpContext.Items.IInternalConfiguration();
        Assert.Same(config, storedConfig);
    }

    [Fact]
    public async Task Invoke_ShouldCallNext_WhenConfigIsNull()
    {
        // Arrange
        _configRepo.Setup(x => x.Get()).Returns((IInternalConfiguration)null);

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        Assert.True(_nextCalled);
        _configRepo.Verify(x => x.Get(), Times.Once);
        // Config is null, nothing is set in context items
        var storedConfig = _httpContext.Items.IInternalConfiguration();
        Assert.Null(storedConfig);
    }

    [Fact]
    public async Task Invoke_ShouldSetConfigInContext_WhenConfigIsNotNull()
    {
        // Arrange
        var config = new Mock<IInternalConfiguration>().Object;
        _configRepo.Setup(x => x.Get()).Returns(config);

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        var storedConfig = _httpContext.Items.IInternalConfiguration();
        Assert.NotNull(storedConfig);
        Assert.Same(config, storedConfig);
    }

    [Fact]
    public async Task Invoke_ShouldNotSetConfigInContext_WhenConfigIsNull()
    {
        // Arrange
        _configRepo.Setup(x => x.Get()).Returns((IInternalConfiguration)null);

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        var storedConfig = _httpContext.Items.IInternalConfiguration();
        Assert.Null(storedConfig);
    }
}
