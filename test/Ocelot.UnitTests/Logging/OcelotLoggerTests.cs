using Microsoft.Extensions.Logging;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;

namespace Ocelot.UnitTests.Logging;

public class OcelotLoggerTests
{
    private readonly Mock<ILogger<object>> _coreLogger;
    private readonly OcelotLogger _logger;

    private static readonly string _a = "Tom";
    private static readonly string _b = "Laura";
    private static readonly Exception _ex = new("oh no");
    private static readonly string NL = Environment.NewLine;

    public OcelotLoggerTests()
    {
        _coreLogger = new Mock<ILogger<object>>();
        _coreLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        var repo = new Mock<IRequestScopedDataRepository>();
        _logger = new OcelotLogger(_coreLogger.Object, repo.Object);
    }

    [Fact]
    public void Should_log_trace()
    {
        // Arrange, Act
        _logger.LogTrace(() => $"a message from {_a} to {_b}");

        // Assert
        ThenLevelIsLogged($"RequestId: -, PreviousRequestId: -{NL}a message from Tom to Laura",
            LogLevel.Trace);
    }

    [Fact]
    public void Should_log_info()
    {
        // Arrange, Act
        _logger.LogInformation(() => $"a message from {_a} to {_b}");

        // Assert
        ThenLevelIsLogged($"RequestId: -, PreviousRequestId: -{NL}a message from Tom to Laura",
            LogLevel.Information);
    }

    [Fact]
    public void Should_log_warning()
    {
        // Arrange, Act
        _logger.LogWarning(() => $"a message from {_a} to {_b}");

        // Assert
        ThenLevelIsLogged($"RequestId: -, PreviousRequestId: -{NL}a message from Tom to Laura",
            LogLevel.Warning);
    }

    [Fact]
    public void Should_log_error()
    {
        // Arrange, Act
        _logger.LogError(() => $"a message from {_a} to {_b}", _ex);

        // Assert
        ThenLevelIsLogged($"RequestId: -, PreviousRequestId: -{NL}a message from Tom to Laura",
            LogLevel.Error, _ex);
    }

    [Fact]
    public void Should_log_critical()
    {
        // Arrange, Act
        _logger.LogCritical(() => $"a message from {_a} to {_b}", _ex);

        // Assert
        ThenLevelIsLogged($"RequestId: -, PreviousRequestId: -{NL}a message from Tom to Laura",
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
        var expected = $"RequestId: -, PreviousRequestId: -{NL}a message from Tom to Laura";

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
        var expected = "requestId: No RequestId, previousRequestId: No PreviousRequestId, message: 'a message from Tom to Laura'";

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
        var expected = $"RequestId: -, PreviousRequestId: -{NL}a message from Tom to Laura";

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
        // Arrange
        var mockedFunc = new Mock<Func<string>>();
        mockedFunc.Setup(x => x.Invoke()).Returns("test").Verifiable();
        var mockedILogger = MockLogger(LogLevel.None);
        var repo = new Mock<IRequestScopedDataRepository>();
        var currentLogger = new OcelotLogger(mockedILogger.Object, repo.Object);

        // Act
        currentLogger.LogTrace(mockedFunc.Object);

        // Assert
        mockedFunc.Verify(x => x.Invoke(), Times.Never);
    }

    [Fact]
    public void String_func_is_called_once_when_log_level_is_enabled()
    {
        // Arrange
        var mockedFunc = new Mock<Func<string>>();
        mockedFunc.Setup(x => x.Invoke()).Returns("test").Verifiable();
        var mockedILogger = MockLogger(LogLevel.Information);
        var repo = new Mock<IRequestScopedDataRepository>();
        var currentLogger = new OcelotLogger(mockedILogger.Object, repo.Object);

        // Act
        currentLogger.LogInformation(mockedFunc.Object);

        // Assert
        mockedFunc.Verify(x => x.Invoke(), Times.Once);
    }

    [Fact]
    public void If_minimum_log_level_set_to_debug_then_log_is_called_for_debug_and_above()
    {
        var mockedILogger = MockLogger(LogLevel.Debug);

        var repo = new Mock<IRequestScopedDataRepository>();

        var currentLogger = new OcelotLogger(mockedILogger.Object, repo.Object);

        currentLogger.LogDebug(() => $"a message from {_a} to {_b}");
        var expected = $"RequestId: -, PreviousRequestId: -{NL}a message from Tom to Laura";

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
        var expected = $"RequestId: -, PreviousRequestId: -{NL}a message from Tom to Laura";

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
        var expected = $"RequestId: -, PreviousRequestId: -{NL}a message from Tom to Laura";

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
        var expected = $"RequestId: -, PreviousRequestId: -{NL}a message from Tom to Laura";

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
