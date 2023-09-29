using Ocelot.Logging;
using Ocelot.Polling;

namespace Ocelot.Provider.Kubernetes;

public class PollKube : ServicePollingHandler<KubernetesServiceDiscoveryProvider>
{
    public PollKube(KubernetesServiceDiscoveryProvider baseProvider, int pollingInterval, string serviceName,
        IOcelotLoggerFactory factory) : base(baseProvider, pollingInterval, serviceName, factory)
    {
    }
}
