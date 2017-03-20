using System;

namespace Ocelot.Logging
{
    public interface IOcelotLoggerFactory
    {
        IOcelotLogger CreateLogger<T>();
    }
    /// <summary>
    /// Thin wrapper around the DotNet core logging framework, used to allow the scopedDataRepository to be injected giving access to the Ocelot RequestId
    /// </summary>
    public interface IOcelotLogger
    {
        void LogTrace(string message, params object[] args);
        void LogDebug(string message, params object[] args);
        void LogError(string message, Exception exception);
        void LogError(string message, params object[] args);

        /// <summary>
        /// The name of the type the logger has been built for.
        /// </summary>
        string Name { get; }
    }
}
