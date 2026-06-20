using Microsoft.Extensions.Logging;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;

namespace Ocelot.UnitTests.Logging;

public class OcelotLoggerFactoryTests
{
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly Mock<IRequestScopedDataRepository> _scopedDataRepositoryMock;
    private readonly OcelotLoggerFactory _factory;

    public OcelotLoggerFactoryTests()
    {
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _scopedDataRepositoryMock = new Mock<IRequestScopedDataRepository>();

        _factory = new OcelotLoggerFactory(
            _loggerFactoryMock.Object,
            _scopedDataRepositoryMock.Object);
    }

    [Fact]
    public void Constructor_GivenNullLoggerFactory_ArgumentNullExceptionThrown()
    {
        // Arrange
        ILoggerFactory loggerFactory = null;

        // Act + Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new OcelotLoggerFactory(loggerFactory, _scopedDataRepositoryMock.Object));

        // Optional: check the parameter name
        Assert.Equal(nameof(loggerFactory), exception.ParamName);
    }

    [Fact]
    public void Constructor_GivenNullScopedDataRepository_ArgumentNullExceptionThrown()
    {
        // Arrange
        IRequestScopedDataRepository scopedDataRepository = null;

        // Act + Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new OcelotLoggerFactory(_loggerFactoryMock.Object, scopedDataRepository!));

        // Optional: check the parameter name
        Assert.Equal(nameof(scopedDataRepository), exception.ParamName);
    }

    [Fact]
    public void CreateLogger_GivenGenericType_LoggerCreated()
    {
        // Arrange
        var logger = new Mock<ILogger>();
        _loggerFactoryMock
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(logger.Object);

        // Act
        var result = _factory.CreateLogger<OcelotLoggerFactoryTests>();

        // Assert
        Assert.NotNull(result);
        _loggerFactoryMock.Verify(
            x => x.CreateLogger(typeof(OcelotLoggerFactoryTests).FullName),
            Times.Once);
    }

    [Fact]
    public void CreateLogger_GivenLoggerFactoryThrows_ExceptionThrown()
    {
        // Arrange
        _loggerFactoryMock
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Throws(new InvalidOperationException("logger creation failed"));

        // Act
        var exception = Record.Exception(() =>
            _factory.CreateLogger<OcelotLoggerFactoryTests>());

        // Assert
        Assert.NotNull(exception);
        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public void CreateLogger_GivenFactoryDisposed_ObjectDisposedExceptionThrown()
    {
        // Arrange
        _factory.Dispose();

        // Act + Assert
        Assert.Throws<ObjectDisposedException>(() =>
            _factory.CreateLogger<OcelotLoggerFactoryTests>());
    }

    [Fact]
    public void CreateLogger_GivenGenericType_OcelotLoggerReturned()
    {
        // Arrange
        var logger = new Mock<ILogger>();
        _loggerFactoryMock
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(logger.Object);

        // Act
        var result = _factory.CreateLogger<OcelotLoggerFactoryTests>();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<OcelotLogger>(result);
    }

    [Fact]
    public void Dispose_GivenFactoryNotDisposed_InnerFactoryDisposed()
    {
        // Arrange

        // Act
        _factory.Dispose();

        // Assert
        _loggerFactoryMock.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_GivenFactoryAlreadyDisposed_NoExceptionThrown()
    {
        // Arrange

        // Act
        var exception = Record.Exception(() =>
        {
            _factory.Dispose();
            _factory.Dispose();
        });

        // Assert
        Assert.Null(exception);
    }
}
