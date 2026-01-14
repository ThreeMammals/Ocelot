using Microsoft.Extensions.Logging;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Responses;

namespace Ocelot.UnitTests.Logging;

public class OcelotLoggerTests
{
    private static readonly string _a = "Tom";
    private static readonly string _b = "Laura";
    private static readonly Exception _ex = new("oh no");
    private static readonly string NL = Environment.NewLine;

    private OcelotLogger _logger;
    private readonly Mock<ILogger<object>> logger = new();
    private readonly Mock<IRequestScopedDataRepository> scopedDataRepository = new();
    public OcelotLoggerTests()
    {
        logger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        scopedDataRepository.Setup(x => x.Get<string>(It.IsAny<string>()))
            .Returns(new OkResponse<string>("1"));
        _logger = new OcelotLogger(logger.Object, scopedDataRepository.Object);
    }

    [Fact]
    public void Ctor_NullChecks()
    {
        // Arrange, Act, Assert: argument 1
        var ex = Assert.Throws<ArgumentNullException>(
            () => _logger = new(null, scopedDataRepository.Object));
        Assert.Equal(nameof(logger), ex.ParamName);

        // Arrange, Act, Assert: argument 2
        ex = Assert.Throws<ArgumentNullException>(
            () => _logger = new(logger.Object, null));
        Assert.Equal(nameof(scopedDataRepository), ex.ParamName);
    }

    [Fact]
    public void GetOcelotRequestId()
    {
        // Arrange, Act, Assert
        scopedDataRepository.Setup(x => x.Get<string>(It.IsAny<string>()))
            .Returns(new OkResponse<string>("X"));
        _logger.LogTrace($"a message from {_a} to {_b}");
        ThenLevelIsLogged($"RequestId: X, PreviousRequestId: X{NL}a message from Tom to Laura",
            LogLevel.Trace, Times.Once());

        scopedDataRepository.Setup(x => x.Get<string>(It.IsAny<string>()))
            .Returns(new ErrorResponse<string>(new CannotFindDataError("error")));
        _logger.LogTrace($"a message from {_a} to {_b}");
        ThenLevelIsLogged($"RequestId: -, PreviousRequestId: -{NL}a message from Tom to Laura",
            LogLevel.Trace, Times.Once());
    }

    [Fact]
    public void Should_log_trace()
    {
        // Arrange, Act, Assert
        _logger.LogTrace(() => $"a message from {_a} to {_b}");
        ThenLevelIsLogged($"RequestId: 1, PreviousRequestId: 1{NL}a message from Tom to Laura",
            LogLevel.Trace, Times.Once());

        _logger.LogTrace($"a message from {_a} to {_b}");
        ThenLevelIsLogged($"RequestId: 1, PreviousRequestId: 1{NL}a message from Tom to Laura",
            LogLevel.Trace, Times.Exactly(2));
    }

    [Fact]
    public void Should_log_debug()
    {
        // Arrange, Act, Assert
        _logger.LogDebug(() => $"a message from {_a} to {_b}");
        ThenLevelIsLogged($"RequestId: 1, PreviousRequestId: 1{NL}a message from Tom to Laura",
            LogLevel.Debug, Times.Once());

        _logger.LogDebug($"a message from {_a} to {_b}");
        ThenLevelIsLogged($"RequestId: 1, PreviousRequestId: 1{NL}a message from Tom to Laura",
            LogLevel.Debug, Times.Exactly(2));
    }

    [Fact]
    public void Should_log_info()
    {
        // Arrange, Act, Assert
        _logger.LogInformation(() => $"a message from {_a} to {_b}");
        ThenLevelIsLogged($"RequestId: 1, PreviousRequestId: 1{NL}a message from Tom to Laura",
            LogLevel.Information, Times.Once());

        _logger.LogInformation($"a message from {_a} to {_b}");
        ThenLevelIsLogged($"RequestId: 1, PreviousRequestId: 1{NL}a message from Tom to Laura",
            LogLevel.Information, Times.Exactly(2));
    }

    [Fact]
    public void Should_log_warning()
    {
        // Arrange, Act, Assert
        _logger.LogWarning(() => $"a message from {_a} to {_b}");
        ThenLevelIsLogged($"RequestId: 1, PreviousRequestId: 1{NL}a message from Tom to Laura",
            LogLevel.Warning, Times.Once());

        _logger.LogWarning($"a message from {_a} to {_b}");
        ThenLevelIsLogged($"RequestId: 1, PreviousRequestId: 1{NL}a message from Tom to Laura",
            LogLevel.Warning, Times.Exactly(2));
    }

    [Fact]
    public void Should_log_error()
    {
        // Arrange, Act, Assert
        _logger.LogError(() => $"a message from {_a} to {_b}", _ex);
        ThenLevelIsLogged($"RequestId: 1, PreviousRequestId: 1{NL}a message from Tom to Laura",
            LogLevel.Error, Times.Once(), _ex);

        _logger.LogError($"a message from {_a} to {_b}", _ex);
        ThenLevelIsLogged($"RequestId: 1, PreviousRequestId: 1{NL}a message from Tom to Laura",
            LogLevel.Error, Times.Exactly(2), _ex);
    }

    [Fact]
    public void Should_log_critical()
    {
        // Arrange, Act, Assert
        _logger.LogCritical(() => $"a message from {_a} to {_b}", _ex);
        ThenLevelIsLogged($"RequestId: 1, PreviousRequestId: 1{NL}a message from Tom to Laura",
            LogLevel.Critical, Times.Once(), _ex);

        _logger.LogCritical($"a message from {_a} to {_b}", _ex);
        ThenLevelIsLogged($"RequestId: 1, PreviousRequestId: 1{NL}a message from Tom to Laura",
            LogLevel.Critical, Times.Exactly(2), _ex);
    }

    [Fact]
    public void StaticFormatters()
    {
        Exception ex = new("test");
        var actual = OcelotLogger.NoFormatter("x", ex);
        Assert.Equal("x", actual);

        actual = OcelotLogger.ExceptionFormatter("y", null);
        Assert.Equal("y", actual);

        actual = OcelotLogger.ExceptionFormatter("z", ex);
        var expected = $"z, {Environment.NewLine}Exception: System.Exception: test";
        Assert.Equal(expected, actual);
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
        var currentLogger = new OcelotLogger(mockedILogger.Object, scopedDataRepository.Object);

        currentLogger.LogDebug(() => $"a message from {_a} to {_b}");
        var expected = $"RequestId: 1, PreviousRequestId: 1{NL}a message from Tom to Laura";

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
        var currentLogger = new OcelotLogger(mockedILogger.Object, scopedDataRepository.Object);

        currentLogger.LogDebug(() => $"a message from {_a} to {_b}");
        var expected = $"RequestId: 1, PreviousRequestId: 1{NL}a message from Tom to Laura";

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
        var currentLogger = new OcelotLogger(mockedILogger.Object, scopedDataRepository.Object);

        // Act
        currentLogger.LogInformation(mockedFunc.Object);

        // Assert
        mockedFunc.Verify(x => x.Invoke(), Times.Once);
    }

    [Fact]
    public void If_minimum_log_level_set_to_debug_then_log_is_called_for_debug_and_above()
    {
        var mockedILogger = MockLogger(LogLevel.Debug);
        var currentLogger = new OcelotLogger(mockedILogger.Object, scopedDataRepository.Object);

        currentLogger.LogDebug(() => $"a message from {_a} to {_b}");
        var expected = $"RequestId: 1, PreviousRequestId: 1{NL}a message from Tom to Laura";

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
        var currentLogger = new OcelotLogger(mockedILogger.Object, scopedDataRepository.Object);

        currentLogger.LogDebug(() => $"a message from {_a} to {_b}");
        var expected = $"RequestId: 1, PreviousRequestId: 1{NL}a message from Tom to Laura";

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
        var currentLogger = new OcelotLogger(mockedILogger.Object, scopedDataRepository.Object);

        currentLogger.LogDebug(() => $"a message from {_a} to {_b}");
        var expected = $"RequestId: 1, PreviousRequestId: 1{NL}a message from Tom to Laura";

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
        var currentLogger = new OcelotLogger(mockedILogger.Object, scopedDataRepository.Object);

        currentLogger.LogDebug(() => $"a message from {_a} to {_b}");
        var expected = $"RequestId: 1, PreviousRequestId: 1{NL}a message from Tom to Laura";

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

    private void ThenLevelIsLogged(string expected, LogLevel expectedLogLevel, Times times, Exception ex = null)
    {
        logger.Verify(
            x => x.Log(expectedLogLevel, default, expected, ex, It.IsAny<Func<string, Exception, string>>()),
            times);
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
