using Ocelot.Logging;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;

namespace Ocelot.Polling;

public abstract class ServicePollingHandler<T> : IServiceDiscoveryProvider
    where T : class, IServiceDiscoveryProvider
{
    private readonly T _baseProvider;
    private readonly object _lockObject = new();
    private readonly IOcelotLogger _logger;
    private readonly int _pollingInterval;

    private DateTime _lastUpdateTime;
    private List<Service> _services;

    protected ServicePollingHandler(T baseProvider, int pollingInterval, string serviceName,
        IOcelotLoggerFactory factory)
    {
        _logger = factory.CreateLogger<ServicePollingHandler<T>>();
        _pollingInterval = pollingInterval;

        // Initialize by DateTime.MinValue as lowest value.
        // Polling will occur immediately during the first call
        _lastUpdateTime = DateTime.MinValue;
        _services = new List<Service>();
        ServiceName = serviceName;
        _baseProvider = baseProvider;
    }

    public string ServiceName { get; protected set; }

    public Task<List<Service>> GetAsync()
    {
        if (_baseProvider == null)
        {
            throw new Exception("Base provider is not initialized. Please call SetParameters method first.");
        }

        lock (_lockObject)
        {
            var refreshTime = _lastUpdateTime.AddMilliseconds(_pollingInterval);

            // Check if any services available
            if (refreshTime >= DateTime.UtcNow && _services.Any())
            {
                return Task.FromResult(_services);
            }

            _logger.LogInformation($"Retrieving new client information for service: {ServiceName}.");
            _services = _baseProvider.GetAsync().Result;
            _lastUpdateTime = DateTime.UtcNow;

            return Task.FromResult(_services);
        }
    }
}
