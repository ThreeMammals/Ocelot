using Microsoft.Extensions.Logging;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Responses;

namespace Ocelot.UnitTests.Logging;

public class OcelotLoggerTestsForDisposal
{
    private readonly Mock<ILogger> _innerLoggerMock;
    private readonly Mock<IRequestScopedDataRepository> _scopedDataRepositoryMock;
    private readonly OcelotLogger _logger;

    public OcelotLoggerTestsForDisposal()
    {
        _innerLoggerMock = new Mock<ILogger>();
        _innerLoggerMock
            .Setup(x => x.IsEnabled(It.IsAny<LogLevel>()))
            .Returns(true);

        _scopedDataRepositoryMock = new Mock<IRequestScopedDataRepository>();
        _scopedDataRepositoryMock
            .Setup(x => x.Get<string>(It.IsAny<string>()))
            .Returns(new OkResponse<string>("ID"));

        _logger = new OcelotLogger(_innerLoggerMock.Object, _scopedDataRepositoryMock.Object);
    }

    [Fact]
    public void Dispose_GivenLoggerDisposedThenLoggingAttempt_NoUnderlyingLoggerCalled()
    {
        // Arrange
        _logger.Dispose();

        // Act
        _logger.LogInformation("should not log");

        // Assert
        _innerLoggerMock.Verify(x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<string>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<string, Exception, string>>()),
            Times.Never);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_NoExceptionThrown()
    {
        // Arrange
        _logger.Dispose();

        // Act & Assert
        Assert.Null(Record.Exception(() =>
        {
            _logger.Dispose();
            _logger.Dispose();
        }));
    }

    [Fact]
    public void LogAfterDisposeWithFunc_GivenDisposed_NoFuncInvocation()
    {
        // Arrange
        var funcMock = new Mock<Func<string>>();
        funcMock.Setup(x => x.Invoke()).Returns("invoked");

        _logger.Dispose();

        // Act
        _logger.LogTrace(funcMock.Object);

        // Assert
        funcMock.Verify(x => x.Invoke(), Times.Never);
        _innerLoggerMock.Verify(x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<string>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<string, Exception, string>>()),
            Times.Never);
    }

    [Fact]
    public void Log_GivenLoggerThrowsObjectDisposedException_NoExceptionEscapes()
    {
        // Arrange
        // Configure the logger to throw ObjectDisposedException when logging
        _innerLoggerMock
            .Setup(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<string>(), It.IsAny<Exception>(), It.IsAny<Func<string, Exception, string>>()))
            .Throws(new ObjectDisposedException($"ILogger {nameof(_innerLoggerMock)}"));

        // Act & Assert: No exception escapes
        var exception = Record.Exception(() =>
            _logger.LogInformation("Test message"));
        Assert.Null(exception);

        // Optional: also verify no call to underlying logger occurred (since Log threw, nothing should be invoked)
        _innerLoggerMock.Verify(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<string>(), It.IsAny<Exception>(), It.IsAny<Func<string, Exception, string>>()),
            Times.Once);
    }
}
