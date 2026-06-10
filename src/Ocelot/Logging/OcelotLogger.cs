using Microsoft.Extensions.Logging;
using Ocelot.Infrastructure.RequestData;
using Ocelot.RequestId.Middleware;

namespace Ocelot.Logging;

/// <summary>
/// Default implementation of the <see cref="IOcelotLogger"/> interface.
/// </summary>
public class OcelotLogger : IOcelotLogger, IDisposable
{
    private readonly ILogger _logger;
    private readonly IRequestScopedDataRepository _scopedDataRepository;
    private volatile bool _disposed;

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
        _scopedDataRepository = scopedDataRepository ?? throw new ArgumentNullException(nameof(scopedDataRepository));
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
        return requestId.IsError ? "-" : requestId.Data;
    }

    private string GetOcelotPreviousRequestId()
    {
        var requestId = _scopedDataRepository.Get<string>(RequestIdMiddleware.PreviousRequestIdName);
        return requestId.IsError ? "-" : requestId.Data;
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
        if (_disposed || !_logger.IsEnabled(logLevel))
            return;

        var requestId = GetOcelotRequestId();
        var previousRequestId = GetOcelotPreviousRequestId();
        if (messageFactory != null)
        {
            message = messageFactory.Invoke();
        }

        try
        {
            _logger.Log(logLevel, default,
                $"{RequestIdMiddleware.RequestIdName}: {requestId}, {RequestIdMiddleware.PreviousRequestIdName}: {previousRequestId}{message}",
                exception, NoFormatter);
        }
        catch (ObjectDisposedException)
        {
            // Logger factory or its providers have been disposed.
            // This can happen when errors occur in background operations.
            // Silently ignore to prevent cascading failures during shutdown.
        }
    }

    public static string NoFormatter(string state, Exception e) => state;
    public static string ExceptionFormatter(string state, Exception e)
        => e == null ? state : $"{state}, {nameof(Exception)}: {e}";

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
