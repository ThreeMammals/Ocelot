using System.Diagnostics;

namespace Ocelot.Logging
{
    /// <summary>
    /// Thin wrapper around the DotNet core logging framework, used to allow the scopedDataRepository to be injected giving access to the Ocelot RequestId.
    /// </summary>
    public interface IOcelotLogger
    {
        void LogTrace(Func<string> messageFactory);
        
        void LogDebug(Func<string> messageFactory);

        void LogInformation(Func<string> messageFactory);

        void LogWarning(Func<string> messageFactory);

        void LogError(Func<string> messageFactory, Exception exception);

        void LogCritical(Func<string> messageFactory, Exception exception);
    }
}
