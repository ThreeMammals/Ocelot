using Ocelot.Logging;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;

namespace Ocelot.Provider.Consul;

public sealed class PollConsul : IServiceDiscoveryProvider
{
    private readonly object _lockObject = new();
    private readonly IOcelotLogger _logger;
    private readonly IServiceDiscoveryProvider _consulServiceDiscoveryProvider;
    private readonly int _pollingInterval;

    private DateTime _lastUpdateTime;
    private List<Service> _services;

    public PollConsul(int pollingInterval, string serviceName, IOcelotLoggerFactory factory,
        IServiceDiscoveryProvider consulServiceDiscoveryProvider)
    {
        _logger = factory.CreateLogger<PollConsul>();
        _consulServiceDiscoveryProvider = consulServiceDiscoveryProvider;
        _pollingInterval = pollingInterval;

        // Initialize by DateTime.MinValue as lowest value.
        // Polling will occur immediately during the first call
        _lastUpdateTime = DateTime.MinValue;

        _services = new List<Service>();
        ServiceName = serviceName;
    }

    public string ServiceName { get; }

    /// <summary>
    /// Gets the services.
    /// <para>If the first call, retrieves the services and then starts the timer.</para>
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> with a <see cref="List{Service}" /> result of <see cref="Service" />.</returns>
    public Task<List<Service>> GetAsync()
    {
        lock (_lockObject)
        {
            var refreshTime = _lastUpdateTime.AddMilliseconds(_pollingInterval);

            // Check if any services available
            if (refreshTime >= DateTime.UtcNow && _services.Any())
            {
                return Task.FromResult(_services);
            }

            try
            {
                _logger.LogInformation(() => $"Retrieving new client information for service: {ServiceName}...");
                _services = _consulServiceDiscoveryProvider.GetAsync().GetAwaiter().GetResult();
                return Task.FromResult(_services);
            }
            finally
            {
                _lastUpdateTime = DateTime.UtcNow;
            }
        }
    }
}
