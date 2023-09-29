using Ocelot.Logging;
using Ocelot.ServiceDiscovery.Providers;

namespace Ocelot.Polling;

public class PollingServicesManager<T, TU>
    where T : class, IServiceDiscoveryProvider
    where TU : ServicePollingHandler<T>
{
    private readonly object _lockObject = new();
    private readonly List<ServicePollingHandler<T>> _serviceDiscoveryProviders = new();

    public ServicePollingHandler<T> GetServicePollingHandler(T baseProvider, string serviceName, int pollingInterval,
        IOcelotLoggerFactory factory)
    {
        lock (_lockObject)
        {
            var discoveryProvider = _serviceDiscoveryProviders.FirstOrDefault(x => x.ServiceName == serviceName);
            if (discoveryProvider != null)
            {
                return discoveryProvider;
            }

            discoveryProvider =
                (TU)Activator.CreateInstance(typeof(TU), baseProvider, pollingInterval, serviceName, factory);
            _serviceDiscoveryProviders.Add(discoveryProvider);

            return (TU)discoveryProvider;
        }
    }
}
