using Ocelot.Logging;

namespace Ocelot.Middleware
{
    public abstract class OcelotMiddleware
    {
        protected OcelotMiddleware(IOcelotLogger logger)
        {
            Logger = logger;
            MiddlewareName = GetType().Name;
        }

        public IOcelotLogger Logger { get; }
        public string MiddlewareName { get; }
    }
}
