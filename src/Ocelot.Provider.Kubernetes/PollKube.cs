using Ocelot.Logging;
using Ocelot.Values;
using System.Collections.Concurrent;

namespace Ocelot.Provider.Kubernetes;

public class PollKube : IServiceDiscoveryProvider, IDisposable
{
    private readonly IOcelotLogger _logger;
    private readonly IServiceDiscoveryProvider _discoveryProvider;
    private readonly ConcurrentQueue<List<Service>> _queue = new();

    private Timer _timer;
    private bool _polling;

    public PollKube(int pollingInterval, IOcelotLoggerFactory factory, IServiceDiscoveryProvider kubeProvider)
    {
        _logger = factory.CreateLogger<PollKube>();
        _discoveryProvider = kubeProvider;
        _timer = new(OnTimerCallbackAsync, null, pollingInterval, pollingInterval);
    }

    private async void OnTimerCallbackAsync(object state)
    {
        // Avoid polling if already in progress due to a slow completion of the Poll task,
        // and ensure no more than three versions of services remain in the queue.
        if (_polling || _queue.Count > 3)
        {
            return;
        }

        _polling = true;
        await Poll();
        _polling = false;
    }

    public async Task<List<Service>> GetAsync()
    {
        // First cold request must call the provider
        if (_queue.IsEmpty)
        {
            return await Poll();
        }
        else if (_polling && _queue.TryPeek(out var oldVersion))
        {
            return oldVersion;
        }

        // For services with multiple versions, remove outdated versions and retain only the latest one
        while (!_polling && _queue.Count > 1 && _queue.TryDequeue(out _))
        {
        }

        return _queue.TryPeek(out var latestVersion)
            ? latestVersion : new(0);
    }

    protected virtual async Task<List<Service>> Poll()
    {
        _polling = true;
        try
        {
            var services = await _discoveryProvider.GetAsync();
            _queue.Enqueue(services);
            return services;
        }
        finally
        {
            _polling = false;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            _timer = null;
        }
    }
}
