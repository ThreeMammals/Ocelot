using Ocelot.Logging;

namespace Ocelot.Middleware;

public abstract class OcelotMiddleware
{
    protected OcelotMiddleware(IOcelotLogger logger)
    {
        Logger = logger;
    }

    public IOcelotLogger Logger { get; }
    public abstract string MiddlewareName { get; }
}
