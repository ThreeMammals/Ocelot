using Ocelot.Logging;

namespace Ocelot.Middleware;

public abstract class OcelotMiddleware
{
    protected OcelotMiddleware(IOcelotLogger logger)
    {
        Logger = logger;
    }

    public IOcelotLogger Logger { get; }
    protected abstract string MiddlewareName { get; }
}
