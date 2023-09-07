using Ocelot.Logging;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace Ocelot.Provider.Consul;

public sealed class PollConsul : IServiceDiscoveryProvider, IDisposable
{
    private readonly IOcelotLogger _logger;
    private readonly IServiceDiscoveryProvider _consulServiceDiscoveryProvider;
    private Timer _timer;
    private bool _polling;
    private readonly IMemoryCache _cache;
    private readonly string _serviceCacheKey;

    public PollConsul(
        int pollingInterval,
        string serviceName,
        IOcelotLoggerFactory factory,
        IServiceDiscoveryProvider consulServiceDiscoveryProvider,
        IMemoryCache cache)
    {
        _logger = factory.CreateLogger<PollConsul>();
        _consulServiceDiscoveryProvider = consulServiceDiscoveryProvider;
        _cache = cache;
        _serviceCacheKey = $"Consul:Services:{serviceName}";

        _timer = new Timer(async x =>
        {
            if (_polling)
            {
                return;
            }

            _polling = true;
            await Poll();
            _polling = false;
        }, null, pollingInterval, pollingInterval);
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _timer = null;
    }

    public async Task<List<Service>> Get()
    {
        if (_cache.TryGetValue<List<Service>>(_serviceCacheKey, out var services))
        {
            return services;
        }

        await Poll();

        return _cache.Get<List<Service>>(_serviceCacheKey);
    }

    private async Task Poll()
    {
        var services = await _consulServiceDiscoveryProvider.Get();
        _cache.Set(_serviceCacheKey, services);
    }
}
