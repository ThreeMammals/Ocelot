using System;

namespace Ocelot.Provider.Consul
{
    using Logging;
    using ServiceDiscovery.Providers;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Values;

    public class PollConsul : IServiceDiscoveryProvider, IDisposable
    {
        private readonly IOcelotLogger _logger;
        private readonly IServiceDiscoveryProvider _consulServiceDiscoveryProvider;
        private readonly Timer _timer;
        private bool _polling;
        private List<Service> _services;

        public PollConsul(int pollingInterval, IOcelotLoggerFactory factory, IServiceDiscoveryProvider consulServiceDiscoveryProvider)
        {
            _logger = factory.CreateLogger<PollConsul>();
            _consulServiceDiscoveryProvider = consulServiceDiscoveryProvider;
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
            }, null, pollingInterval, pollingInterval);
        }

        public Task<List<Service>> Get()
        {
            return Task.FromResult(_services);
        }

        private async Task Poll()
        {
            if (_consulServiceDiscoveryProvider == null)
            {
                return;
            }

            _services = await _consulServiceDiscoveryProvider.Get();
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
