using Microsoft.Extensions.Logging;
using Ocelot.Infrastructure.RequestData;
using Ocelot.RequestId.Middleware;

namespace Ocelot.Logging;

/// <summary>
/// Default implementation of the <see cref="IOcelotLogger"/> interface.
/// </summary>
public class OcelotLogger : IOcelotLogger
{
    private readonly ILogger _logger;
    private readonly IRequestScopedDataRepository _scopedDataRepository;
    private readonly Func<string, Exception, string> _func;

    /// <summary>
    /// Initializes a new instance of the <see cref="OcelotLogger"/> class.
    /// <para>
    /// Please note:
    /// the log event message is designed to use placeholders ({RequestId}, {PreviousRequestId}, and {Message}).
    /// If you're using a logger like Serilog, it will automatically capture these as structured data properties, making it easier to query and analyze the logs later.
    /// </para>
    /// </summary>
    /// <param name="logger">The main logger type, per default the Microsoft implementation.</param>
    /// <param name="scopedDataRepository">Repository, saving and getting data to/from HttpContext.Items.</param>
    /// <exception cref="ArgumentNullException">The ILogger object is injected in OcelotLoggerFactory, it can't be verified before.</exception>
    public OcelotLogger(ILogger logger, IRequestScopedDataRepository scopedDataRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scopedDataRepository = scopedDataRepository;
        _func = (state, exception) => exception == null ? state : $"{state}, {nameof(exception)}: {exception}";
    }

    public void LogTrace(string message) => WriteLog(LogLevel.Trace, message);
    public void LogTrace(Func<string> messageFactory) => WriteLog(LogLevel.Trace, messageFactory);

    public void LogDebug(string message) => WriteLog(LogLevel.Debug, message);
    public void LogDebug(Func<string> messageFactory) => WriteLog(LogLevel.Debug, messageFactory);

    public void LogInformation(string message) => WriteLog(LogLevel.Information, message);
    public void LogInformation(Func<string> messageFactory) => WriteLog(LogLevel.Information, messageFactory);

    public void LogWarning(string message) => WriteLog(LogLevel.Warning, message);
    public void LogWarning(Func<string> messageFactory) => WriteLog(LogLevel.Warning, messageFactory);

    public void LogError(string message, Exception exception) => WriteLog(LogLevel.Error, message, exception);
    public void LogError(Func<string> messageFactory, Exception exception) => WriteLog(LogLevel.Error, messageFactory, exception);

    public void LogCritical(string message, Exception exception) => WriteLog(LogLevel.Critical, message, exception);
    public void LogCritical(Func<string> messageFactory, Exception exception) => WriteLog(LogLevel.Critical, messageFactory, exception);

    private string GetOcelotRequestId()
    {
        var requestId = _scopedDataRepository.Get<string>(RequestIdMiddleware.RequestIdName);
        return requestId?.IsError ?? true ? $"No {RequestIdMiddleware.RequestIdName}" : requestId.Data;
    }

    private string GetOcelotPreviousRequestId()
    {
        var requestId = _scopedDataRepository.Get<string>(RequestIdMiddleware.PreviousRequestIdName);
        return requestId?.IsError ?? true ? $"No {RequestIdMiddleware.PreviousRequestIdName}" : requestId.Data;
    }

    private void WriteLog(LogLevel logLevel, string message, Exception exception = null)
    {
        WriteLog(logLevel, null, message, exception);
    }

    private void WriteLog(LogLevel logLevel, Func<string> messageFactory, Exception exception = null)
    {
        WriteLog(logLevel, messageFactory, null, exception);
    }

    private void WriteLog(LogLevel logLevel, Func<string> messageFactory, string message, Exception exception = null)
    {
        if (!_logger.IsEnabled(logLevel))
        {
            return;
        }

        var requestId = GetOcelotRequestId();
        var previousRequestId = GetOcelotPreviousRequestId();

        if (messageFactory != null)
        {
            message = messageFactory.Invoke() ?? string.Empty;
        }

        _logger.Log(logLevel,
            default,
            $"{nameof(requestId)}: {requestId}, {nameof(previousRequestId)}: {previousRequestId}, {nameof(message)}: '{message}'",
            exception,
            _func);
    }
}
