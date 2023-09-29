using Ocelot.Logging;
using Ocelot.Polling;

namespace Ocelot.Provider.Eureka;

public class PollEureka : ServicePollingHandler<Eureka>
{
    public PollEureka(Eureka baseProvider, int pollingInterval, string serviceName, IOcelotLoggerFactory factory) : base(baseProvider, pollingInterval, serviceName, factory)
    {
    }
}
