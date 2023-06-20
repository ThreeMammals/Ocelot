using Ocelot.Logging;
using Ocelot.ServiceDiscovery.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ocelot.Values;

namespace Ocelot.Provider.Consul
{
    public sealed class PollConsul : IServiceDiscoveryProvider, IDisposable
    {
        private readonly IOcelotLogger _logger;
        private readonly IServiceDiscoveryProvider _consulServiceDiscoveryProvider;

        private readonly int _pollingInterval;
        private DateTime _lastUpdateTime;

        private List<Service> _services;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public PollConsul(int pollingInterval, string serviceName, IOcelotLoggerFactory factory, IServiceDiscoveryProvider consulServiceDiscoveryProvider)
        {
            _logger = factory.CreateLogger<PollConsul>();
            _consulServiceDiscoveryProvider = consulServiceDiscoveryProvider;
            _pollingInterval = pollingInterval;
            ServiceName = serviceName;
            _services = new List<Service>();

            //initializing with update time = DateTime.MinValue
            //polling will occur then.
            _lastUpdateTime = DateTime.MinValue;
        }

        public string ServiceName { get; }

        /// <summary>
        /// Getting the services, but, if first call,
        /// retrieving the services and then starting the timer.
        /// </summary>
        /// <returns>the service list.</returns>
        public async Task<List<Service>> Get()
        {
            await _semaphore.WaitAsync();
            try
            {
                var refreshTime = _lastUpdateTime.AddMilliseconds(_pollingInterval);

                //checking if any services available
                if (refreshTime >= DateTime.UtcNow && _services.Any())
                {
                    return _services;
                }

                _logger.LogInformation($"Retrieving new client information for service: {ServiceName}");
                _services = await _consulServiceDiscoveryProvider.Get();
                _lastUpdateTime = DateTime.UtcNow;

                return _services;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Dispose()
        {
            _semaphore.Dispose();
        }
    }
}
