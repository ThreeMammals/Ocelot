using Ocelot.Logging;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ocelot.Provider.Consul;

public sealed class PollConsul : IServiceDiscoveryProvider, IDisposable
{
    private readonly IOcelotLogger _logger;
    private readonly IServiceDiscoveryProvider _provider;
    private Timer _timer;
    private bool _polling;
    private List<Service> _services;

    public PollConsul(int pollingInterval, IOcelotLoggerFactory factory, IServiceDiscoveryProvider consulServiceDiscoveryProvider)
    {
        _logger = factory.CreateLogger<PollConsul>();
        _provider = consulServiceDiscoveryProvider;
        _services = new List<Service>();

        _timer = new Timer(async x =>
        {
            if (_polling)
            {
                return;
            }

            _polling = true;
            await Poll();
            _polling = false;
        }, null, 0, pollingInterval); // the dueTime parameter is 0 (zero) to start timer callback (task) immediately
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _timer = null;
    }

    public Task<List<Service>> Get()
    {
        return Task.FromResult(_services);
    }

    private async Task Poll()
    {
        _services = await _provider.Get();
    }
}
