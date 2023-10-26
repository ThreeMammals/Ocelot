using Microsoft.Extensions.Logging;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;

namespace Ocelot.UnitTests.Logging;

public class AspDotNetLoggerTests
{
    private readonly Mock<ILogger<object>> _coreLogger;
    private readonly Mock<IRequestScopedDataRepository> _repo;
    private readonly AspDotNetLogger _logger;
    private readonly string _b;
    private readonly string _a;
    private readonly Exception _ex;

    public AspDotNetLoggerTests()
    {
        _a = "tom";
        _b = "laura";
        _ex = new Exception("oh no");
        _coreLogger = new Mock<ILogger<object>>();
        _coreLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        _repo = new Mock<IRequestScopedDataRepository>();
        _logger = new AspDotNetLogger(_coreLogger.Object, _repo.Object);
    }

    [Fact]
    public void should_log_trace()
    {
        _logger.LogTrace(() => $"a message from {_a} to {_b}");

        ThenLevelIsLogged(
            "requestId: no request id, previousRequestId: no previous request id, message: a message from tom to laura",
            LogLevel.Trace);
    }

    [Fact]
    public void should_log_info()
    {
        _logger.LogInformation(() => $"a message from {_a} to {_b}");

        ThenLevelIsLogged(
            "requestId: no request id, previousRequestId: no previous request id, message: a message from tom to laura",
            LogLevel.Information);
    }

    [Fact]
    public void should_log_warning()
    {
        _logger.LogWarning(() => $"a message from {_a} to {_b}");

        ThenLevelIsLogged(
            "requestId: no request id, previousRequestId: no previous request id, message: a message from tom to laura",
            LogLevel.Warning);
    }

    [Fact]
    public void should_log_error()
    {
        _logger.LogError(() => $"a message from {_a} to {_b}", _ex);

        ThenLevelIsLogged(
            "requestId: no request id, previousRequestId: no previous request id, message: a message from tom to laura",
            LogLevel.Error, _ex);
    }

    [Fact]
    public void should_log_critical()
    {
        _logger.LogCritical(() => $"a message from {_a} to {_b}", _ex);

        ThenLevelIsLogged(
            "requestId: no request id, previousRequestId: no previous request id, message: a message from tom to laura",
            LogLevel.Critical, _ex);
    }

    [Fact]
    public void if_minimum_log_level_not_set_then_no_logs_are_written()
    {
        var mockedILogger = new Mock<ILogger<object>>();

        var repo = new Mock<IRequestScopedDataRepository>();

        var currentLogger = new AspDotNetLogger(mockedILogger.Object, repo.Object);

        currentLogger.LogDebug(() => $"a message from {_a} to {_b}");

        ThenLevelIsNotLogged(mockedILogger,
            "requestId: no request id, previousRequestId: no previous request id, message: a message from tom to laura",
            LogLevel.Debug);

        currentLogger.LogTrace(() => $"a message from {_a} to {_b}");

        ThenLevelIsNotLogged(mockedILogger,
            "requestId: no request id, previousRequestId: no previous request id, message: a message from tom to laura",
            LogLevel.Trace);

        currentLogger.LogInformation(() => $"a message from {_a} to {_b}");

        ThenLevelIsNotLogged(mockedILogger,
            "requestId: no request id, previousRequestId: no previous request id, message: a message from tom to laura",
            LogLevel.Information);

        currentLogger.LogWarning(() => $"a message from {_a} to {_b}");

        ThenLevelIsNotLogged(mockedILogger,
            "requestId: no request id, previousRequestId: no previous request id, message: a message from tom to laura",
            LogLevel.Warning);

        currentLogger.LogError(() => $"a message from {_a} to {_b}", new Exception("test"));

        ThenLevelIsNotLogged(mockedILogger,
            "requestId: no request id, previousRequestId: no previous request id, message: a message from tom to laura",
            LogLevel.Error);

        currentLogger.LogCritical(() => $"a message from {_a} to {_b}", new Exception("test"));

        ThenLevelIsNotLogged(mockedILogger,
            "requestId: no request id, previousRequestId: no previous request id, message: a message from tom to laura",
            LogLevel.Critical);
    }

    [Fact]
    public void if_minimum_log_level_set_to_none_then_no_logs_are_written()
    {
        var mockedILogger = new Mock<ILogger<object>>();
        mockedILogger.Setup(x => x.IsEnabled(It.Is<LogLevel>(y => y == LogLevel.None))).Returns(true);

        var repo = new Mock<IRequestScopedDataRepository>();

        var currentLogger = new AspDotNetLogger(mockedILogger.Object, repo.Object);

        currentLogger.LogDebug(() => $"a message from {_a} to {_b}");

        ThenLevelIsNotLogged(mockedILogger,
            "requestId: no request id, previousRequestId: no previous request id, message: a message from tom to laura",
            LogLevel.Debug);

        currentLogger.LogTrace(() => $"a message from {_a} to {_b}");

        ThenLevelIsNotLogged(mockedILogger,
            "requestId: no request id, previousRequestId: no previous request id, message: a message from tom to laura",
            LogLevel.Trace);

        currentLogger.LogInformation(() => $"a message from {_a} to {_b}");

        ThenLevelIsNotLogged(mockedILogger,
            "requestId: no request id, previousRequestId: no previous request id, message: a message from tom to laura",
            LogLevel.Information);

        currentLogger.LogWarning(() => $"a message from {_a} to {_b}");

        ThenLevelIsNotLogged(mockedILogger,
            "requestId: no request id, previousRequestId: no previous request id, message: a message from tom to laura",
            LogLevel.Warning);

        currentLogger.LogError(() => $"a message from {_a} to {_b}", new Exception("test"));

        ThenLevelIsNotLogged(mockedILogger,
            "requestId: no request id, previousRequestId: no previous request id, message: a message from tom to laura",
            LogLevel.Error);

        currentLogger.LogCritical(() => $"a message from {_a} to {_b}", new Exception("test"));

        ThenLevelIsNotLogged(mockedILogger,
            "requestId: no request id, previousRequestId: no previous request id, message: a message from tom to laura",
            LogLevel.Critical);
    }

    private void ThenLevelIsLogged(string expected, LogLevel expectedLogLevel, Exception ex = null)
    {
        _coreLogger.Verify(
            x => x.Log(
                expectedLogLevel,
                default,
                expected,
                ex,
                It.IsAny<Func<string, Exception, string>>()), Times.Once);
    }

    private void ThenLevelIsNotLogged(Mock<ILogger<object>> logger, string expected, LogLevel expectedLogLevel, Exception ex = null)
    {
        logger.Verify(
            x => x.Log(
                expectedLogLevel,
                default,
                expected,
                ex,
                It.IsAny<Func<string, Exception, string>>()), Times.Never);
    }
}
