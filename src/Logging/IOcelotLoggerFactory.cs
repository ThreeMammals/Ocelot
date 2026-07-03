namespace Ocelot.Logging;

public interface IOcelotLoggerFactory : IDisposable
{
    IOcelotLogger CreateLogger<T>();
}
