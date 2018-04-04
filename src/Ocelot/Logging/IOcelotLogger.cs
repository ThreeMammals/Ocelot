using System;

namespace Ocelot.Logging
{
    /// <summary>
    /// Thin wrapper around the DotNet core logging framework, used to allow the scopedDataRepository to be injected giving access to the Ocelot RequestId
    /// </summary>
    public interface IOcelotLogger
    {
        void LogTrace(string message, params object[] args);
        void LogDebug(string message, params object[] args);
        void LogInformation(string message, params object[] args);
        void LogWarning(string message, params object[] args);
        void LogError(string message, Exception exception);
        void LogCritical(string message, Exception exception);

        /// <summary>
        /// The name of the type the logger has been built for.
        /// </summary>
        string Name { get; }
    }
}
