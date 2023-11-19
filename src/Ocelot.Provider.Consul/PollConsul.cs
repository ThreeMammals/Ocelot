using Ocelot.Logging;
using Ocelot.Polling;

namespace Ocelot.Provider.Consul;

public class PollConsul : ServicePollingHandler<Consul>
{
    public PollConsul(Consul baseProvider, int pollingInterval, string serviceName, IOcelotLoggerFactory factory) : base(baseProvider, pollingInterval, serviceName, factory)
    {
    }
}
