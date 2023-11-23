using Ocelot.Configuration;
using Ocelot.Infrastructure.RequestData;

namespace Ocelot.Logging;

/// <summary>
/// Thin wrapper around the .NET Core logging framework, used to allow the <see cref="IRequestScopedDataRepository"/> object to be injected giving access to the Ocelot <see cref="IInternalConfiguration.RequestId"/>.
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
