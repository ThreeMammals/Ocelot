namespace Ocelot.Logging;

/// <summary>
/// Thin wrapper around the DotNet core logging framework, used to allow the scopedDataRepository to be injected giving access to the Ocelot RequestId.
/// </summary>
public interface IOcelotLogger
{
    void LogTrace(string message);
    void LogTrace(Func<string> messageFactory);

    void LogDebug(string message);
    void LogDebug(Func<string> messageFactory);

    void LogInformation(string message);
    void LogInformation(Func<string> messageFactory);

    void LogWarning(string message);
    void LogWarning(Func<string> messageFactory);

    void LogError(string message, Exception exception);
    void LogError(Func<string> messageFactory, Exception exception);

    void LogCritical(string message, Exception exception);
    void LogCritical(Func<string> messageFactory, Exception exception);
}
