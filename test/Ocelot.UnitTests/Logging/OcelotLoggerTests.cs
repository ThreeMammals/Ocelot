using Microsoft.Extensions.Logging;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;

namespace Ocelot.UnitTests.Logging;

public class OcelotLoggerTests
{
    private readonly Mock<ILogger<object>> _coreLogger;
    private readonly OcelotLogger _logger;
    private readonly string _b;
    private readonly string _a;
    private readonly Exception _ex;

    public OcelotLoggerTests()
    {
        _a = "tom";
        _b = "laura";
        _ex = new Exception("oh no");
        _coreLogger = new Mock<ILogger<object>>();
        _coreLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        var repo = new Mock<IRequestScopedDataRepository>();
        _logger = new OcelotLogger(_coreLogger.Object, repo.Object);
    }

    [Fact]
    public void Should_log_trace()
    {
        _logger.LogTrace(() => $"a message from {_a} to {_b}");

        ThenLevelIsLogged(
            "requestId: No RequestId, previousRequestId: No PreviousRequestId, message: 'a message from tom to laura'",
            LogLevel.Trace);
    }

    [Fact]
    public void Should_log_info()
    {
        _logger.LogInformation(() => $"a message from {_a} to {_b}");

        ThenLevelIsLogged(
            "requestId: No RequestId, previousRequestId: No PreviousRequestId, message: 'a message from tom to laura'",
            LogLevel.Information);
    }

    [Fact]
    public void Should_log_warning()
    {
        _logger.LogWarning(() => $"a message from {_a} to {_b}");

        ThenLevelIsLogged(
            "requestId: No RequestId, previousRequestId: No PreviousRequestId, message: 'a message from tom to laura'",
            LogLevel.Warning);
    }

    [Fact]
    public void Should_log_error()
    {
        _logger.LogError(() => $"a message from {_a} to {_b}", _ex);

        ThenLevelIsLogged(
            "requestId: No RequestId, previousRequestId: No PreviousRequestId, message: 'a message from tom to laura'",
            LogLevel.Error, _ex);
    }

    [Fact]
    public void Should_log_critical()
    {
        _logger.LogCritical(() => $"a message from {_a} to {_b}", _ex);

        ThenLevelIsLogged(
            "requestId: No RequestId, previousRequestId: No PreviousRequestId, message: 'a message from tom to laura'",
            LogLevel.Critical, _ex);
    }

    /// <summary>
    /// Here mocking the original logger implementation to verify <see cref="ILogger.IsEnabled(LogLevel)"/> calls.
    /// </summary>
    /// <param name="minimumLevel">The chosen minimum log level.</param>
    /// <returns>A mocked <see cref="ILogger"/> object.</returns>
    private static Mock<ILogger<object>> MockLogger(LogLevel? minimumLevel)
    {
        var logger = LoggerFactory.Create(builder =>
            {
                if (minimumLevel.HasValue)
                {
                    builder
                        .AddSimpleConsole()
                        .SetMinimumLevel(minimumLevel.Value);
                }
                else
                {
                    builder.AddSimpleConsole();
                }
            })
            .CreateLogger<ILogger<object>>();

        var mockedILogger = new Mock<ILogger<object>>();
        mockedILogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>()))
            .Returns(logger.IsEnabled)
            .Verifiable();

        return mockedILogger;
    }

    [Fact]
    public void If_minimum_log_level_not_set_then_log_is_called_for_information_and_above()
    {
        var mockedILogger = MockLogger(null);
        var repo = new Mock<IRequestScopedDataRepository>();

        var currentLogger = new OcelotLogger(mockedILogger.Object, repo.Object);

        currentLogger.LogDebug(() => $"a message from {_a} to {_b}");
        var expected = "requestId: No RequestId, previousRequestId: No PreviousRequestId, message: 'a message from tom to laura'";

        ThenLevelIsNotLogged(mockedILogger, expected, LogLevel.Debug);

        currentLogger.LogTrace(() => $"a message from {_a} to {_b}");

        ThenLevelIsNotLogged(mockedILogger, expected, LogLevel.Trace);

        currentLogger.LogInformation(() => $"a message from {_a} to {_b}");

        ThenLevelIsLogged(mockedILogger, expected, LogLevel.Information);

        currentLogger.LogWarning(() => $"a message from {_a} to {_b}");

        ThenLevelIsLogged(mockedILogger, expected, LogLevel.Warning);

        var testException = new Exception("test");

        currentLogger.LogError(() => $"a message from {_a} to {_b}", testException);

        ThenLevelIsLogged(mockedILogger, expected, LogLevel.Error, testException);

        currentLogger.LogCritical(() => $"a message from {_a} to {_b}", testException);

        ThenLevelIsLogged(mockedILogger, expected, LogLevel.Critical, testException);
    }

    [Fact]
    public void If_minimum_log_level_set_to_none_then_log_method_is_never_called()
    {
        var mockedILogger = MockLogger(LogLevel.None);

        var repo = new Mock<IRequestScopedDataRepository>();

        var currentLogger = new OcelotLogger(mockedILogger.Object, repo.Object);

        currentLogger.LogDebug(() => $"a message from {_a} to {_b}");
        var expected = "requestId: No RequestId, previousRequestId: No PreviousRequestId, message: 'a message from tom to laura'";

        ThenLevelIsNotLogged(mockedILogger, expected, LogLevel.Debug);

        currentLogger.LogTrace(() => $"a message from {_a} to {_b}");

        ThenLevelIsNotLogged(mockedILogger, expected, LogLevel.Trace);

        currentLogger.LogInformation(() => $"a message from {_a} to {_b}");

        ThenLevelIsNotLogged(mockedILogger, expected, LogLevel.Information);

        currentLogger.LogWarning(() => $"a message from {_a} to {_b}");

        ThenLevelIsNotLogged(mockedILogger, expected, LogLevel.Warning);

        var testException = new Exception("test");

        currentLogger.LogError(() => $"a message from {_a} to {_b}", testException);

        ThenLevelIsNotLogged(mockedILogger, expected, LogLevel.Error, testException);

        currentLogger.LogCritical(() => $"a message from {_a} to {_b}", testException);

        ThenLevelIsNotLogged(mockedILogger, expected, LogLevel.Critical, testException);
    }

    [Fact]
    public void If_minimum_log_level_set_to_trace_then_log_is_called_for_trace_and_above()
    {
        var mockedILogger = MockLogger(LogLevel.Trace);

        var repo = new Mock<IRequestScopedDataRepository>();

        var currentLogger = new OcelotLogger(mockedILogger.Object, repo.Object);

        currentLogger.LogDebug(() => $"a message from {_a} to {_b}");
        var expected = "requestId: No RequestId, previousRequestId: No PreviousRequestId, message: 'a message from tom to laura'";

        ThenLevelIsLogged(mockedILogger, expected, LogLevel.Debug);

        currentLogger.LogTrace(() => $"a message from {_a} to {_b}");

        ThenLevelIsLogged(mockedILogger, expected, LogLevel.Trace);

        currentLogger.LogInformation(() => $"a message from {_a} to {_b}");

        ThenLevelIsLogged(mockedILogger, expected, LogLevel.Information);

        currentLogger.LogWarning(() => $"a message from {_a} to {_b}");

        ThenLevelIsLogged(mockedILogger, expected, LogLevel.Warning);

        var testException = new Exception("test");

        currentLogger.LogError(() => $"a message from {_a} to {_b}", testException);

        ThenLevelIsLogged(mockedILogger, expected, LogLevel.Error, testException);

        currentLogger.LogCritical(() => $"a message from {_a} to {_b}", testException);

        ThenLevelIsLogged(mockedILogger, expected, LogLevel.Critical, testException);
    }

    [Fact]
    public void String_func_is_never_called_when_log_level_is_disabled()
    {
        var mockedFunc = new Mock<Func<string>>();
        mockedFunc.Setup(x => x.Invoke()).Returns("test").Verifiable();
        var mockedILogger = MockLogger(LogLevel.None);
        var repo = new Mock<IRequestScopedDataRepository>();
        var currentLogger = new OcelotLogger(mockedILogger.Object, repo.Object);

        currentLogger.LogTrace(mockedFunc.Object);

        mockedFunc.Verify(x => x.Invoke(), Times.Never);
    }

    [Fact]
    public void String_func_is_called_once_when_log_level_is_enabled()
    {
        var mockedFunc = new Mock<Func<string>>();
        mockedFunc.Setup(x => x.Invoke()).Returns("test").Verifiable();
        var mockedILogger = MockLogger(LogLevel.Information);
        var repo = new Mock<IRequestScopedDataRepository>();
        var currentLogger = new OcelotLogger(mockedILogger.Object, repo.Object);

        currentLogger.LogInformation(mockedFunc.Object);

        mockedFunc.Verify(x => x.Invoke(), Times.Once);
    }

    [Fact]
    public void If_minimum_log_level_set_to_debug_then_log_is_called_for_debug_and_above()
    {
        var mockedILogger = MockLogger(LogLevel.Debug);

        var repo = new Mock<IRequestScopedDataRepository>();

        var currentLogger = new OcelotLogger(mockedILogger.Object, repo.Object);

        currentLogger.LogDebug(() => $"a message from {_a} to {_b}");
        var expected = "requestId: No RequestId, previousRequestId: No PreviousRequestId, message: 'a message from tom to laura'";

        ThenLevelIsLogged(mockedILogger, expected, LogLevel.Debug);

        currentLogger.LogTrace(() => $"a message from {_a} to {_b}");

        ThenLevelIsNotLogged(mockedILogger, expected, LogLevel.Trace);

        currentLogger.LogInformation(() => $"a message from {_a} to {_b}");

        ThenLevelIsLogged(mockedILogger, expected, LogLevel.Information);

        currentLogger.LogWarning(() => $"a message from {_a} to {_b}");

        ThenLevelIsLogged(mockedILogger, expected, LogLevel.Warning);

        var testException = new Exception("test");

        currentLogger.LogError(() => $"a message from {_a} to {_b}", testException);

        ThenLevelIsLogged(mockedILogger, expected, LogLevel.Error, testException);

        currentLogger.LogCritical(() => $"a message from {_a} to {_b}", testException);

        ThenLevelIsLogged(mockedILogger, expected, LogLevel.Critical, testException);
    }

    [Fact]
    public void If_minimum_log_level_set_to_warning_then_log_is_called_for_warning_and_above()
    {
        var mockedILogger = MockLogger(LogLevel.Warning);

        var repo = new Mock<IRequestScopedDataRepository>();

        var currentLogger = new OcelotLogger(mockedILogger.Object, repo.Object);

        currentLogger.LogDebug(() => $"a message from {_a} to {_b}");
        var expected = "requestId: No RequestId, previousRequestId: No PreviousRequestId, message: 'a message from tom to laura'";

        ThenLevelIsNotLogged(mockedILogger, expected, LogLevel.Debug);

        currentLogger.LogTrace(() => $"a message from {_a} to {_b}");

        ThenLevelIsNotLogged(mockedILogger, expected, LogLevel.Trace);

        currentLogger.LogInformation(() => $"a message from {_a} to {_b}");

        ThenLevelIsNotLogged(mockedILogger, expected, LogLevel.Information);

        currentLogger.LogWarning(() => $"a message from {_a} to {_b}");

        ThenLevelIsLogged(mockedILogger, expected, LogLevel.Warning);

        var testException = new Exception("test");

        currentLogger.LogError(() => $"a message from {_a} to {_b}", testException);

        ThenLevelIsLogged(mockedILogger, expected, LogLevel.Error, testException);

        currentLogger.LogCritical(() => $"a message from {_a} to {_b}", testException);

        ThenLevelIsLogged(mockedILogger, expected, LogLevel.Critical, testException);
    }

    [Fact]
    public void If_minimum_log_level_set_to_error_then_log_is_called_for_error_and_above()
    {
        var mockedILogger = MockLogger(LogLevel.Error);

        var repo = new Mock<IRequestScopedDataRepository>();

        var currentLogger = new OcelotLogger(mockedILogger.Object, repo.Object);

        currentLogger.LogDebug(() => $"a message from {_a} to {_b}");
        var expected = "requestId: No RequestId, previousRequestId: No PreviousRequestId, message: 'a message from tom to laura'";

        ThenLevelIsNotLogged(mockedILogger, expected, LogLevel.Debug);

        currentLogger.LogTrace(() => $"a message from {_a} to {_b}");

        ThenLevelIsNotLogged(mockedILogger, expected, LogLevel.Trace);

        currentLogger.LogInformation(() => $"a message from {_a} to {_b}");

        ThenLevelIsNotLogged(mockedILogger, expected, LogLevel.Information);

        currentLogger.LogWarning(() => $"a message from {_a} to {_b}");

        ThenLevelIsNotLogged(mockedILogger, expected, LogLevel.Warning);

        var testException = new Exception("test");

        currentLogger.LogError(() => $"a message from {_a} to {_b}", testException);

        ThenLevelIsLogged(mockedILogger, expected, LogLevel.Error, testException);

        currentLogger.LogCritical(() => $"a message from {_a} to {_b}", testException);

        ThenLevelIsLogged(mockedILogger, expected, LogLevel.Critical, testException);
    }

    [Fact]
    public void If_minimum_log_level_set_to_critical_then_log_is_called_for_critical_and_above()
    {
        var mockedILogger = MockLogger(LogLevel.Critical);

        var repo = new Mock<IRequestScopedDataRepository>();

        var currentLogger = new OcelotLogger(mockedILogger.Object, repo.Object);

        currentLogger.LogDebug(() => $"a message from {_a} to {_b}");
        var expected = "requestId: No RequestId, previousRequestId: No PreviousRequestId, message: 'a message from tom to laura'";

        ThenLevelIsNotLogged(mockedILogger, expected, LogLevel.Debug);

        currentLogger.LogTrace(() => $"a message from {_a} to {_b}");

        ThenLevelIsNotLogged(mockedILogger, expected, LogLevel.Trace);

        currentLogger.LogInformation(() => $"a message from {_a} to {_b}");

        ThenLevelIsNotLogged(mockedILogger, expected, LogLevel.Information);

        currentLogger.LogWarning(() => $"a message from {_a} to {_b}");

        ThenLevelIsNotLogged(mockedILogger, expected, LogLevel.Warning);

        var testException = new Exception("test");

        currentLogger.LogError(() => $"a message from {_a} to {_b}", testException);

        ThenLevelIsNotLogged(mockedILogger, expected, LogLevel.Error, testException);

        currentLogger.LogCritical(() => $"a message from {_a} to {_b}", testException);

        ThenLevelIsLogged(mockedILogger, expected, LogLevel.Critical, testException);
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

    private static void ThenLevelIsLogged(Mock<ILogger<object>> logger, string expected, LogLevel expectedLogLevel, Exception ex = null)
    {
        logger.Verify(
            x => x.Log(
                expectedLogLevel,
                default,
                expected,
                ex,
                It.IsAny<Func<string, Exception, string>>()), Times.Once);
    }

    private static void ThenLevelIsNotLogged(Mock<ILogger<object>> logger, string expected, LogLevel expectedLogLevel, Exception ex = null)
    {
        var result = logger.Object.IsEnabled(expectedLogLevel);

        logger.Verify(
            x => x.Log(
                expectedLogLevel,
                default,
                expected,
                ex,
                It.IsAny<Func<string, Exception, string>>()), Times.Never);
    }
}
