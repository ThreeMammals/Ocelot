using Microsoft.Extensions.Logging;
using Ocelot.Infrastructure.RequestData;

namespace Ocelot.Logging;

public class OcelotLogger : IOcelotLogger
{
    private readonly ILogger _logger;
    private readonly IRequestScopedDataRepository _scopedDataRepository;
    private readonly Func<string, Exception, string> _func;

    public OcelotLogger(ILogger logger, IRequestScopedDataRepository scopedDataRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scopedDataRepository = scopedDataRepository;
        _func = (state, exception) => exception == null ? state : $"{state}, exception: {exception}";
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
        var requestId = _scopedDataRepository.Get<string>("RequestId");
        return requestId?.IsError ?? true ? "no request id" : requestId.Data;
    }

    private string GetOcelotPreviousRequestId()
    {
        var requestId = _scopedDataRepository.Get<string>("PreviousRequestId");
        return requestId?.IsError ?? true ? "no previous request id" : requestId.Data;
    }

    private void WriteLog(LogLevel logLevel, string message, Exception exception = null)
    {
        string MessageFactory() => message;
        WriteLog(logLevel, MessageFactory, exception);
    }

    private void WriteLog(LogLevel logLevel, Func<string> messageFactory, Exception exception = null)
    {
        if (!_logger.IsEnabled(logLevel))
        {
            return;
        }

        var requestId = GetOcelotRequestId();
        var previousRequestId = GetOcelotPreviousRequestId();
        var msg = messageFactory?.Invoke() ?? string.Empty;

        // the log event message is designed to use placeholders ({RequestId}, {PreviousRequestId}, and {Message}).
        // If you're using a logger like Serilog, it will automatically capture these as structured data properties,
        // making it easier to query and analyze the logs later
        _logger.Log(logLevel, default, $"requestId: {requestId}, previousRequestId: {previousRequestId}, message: '{msg}'", exception, _func);
    }
}
