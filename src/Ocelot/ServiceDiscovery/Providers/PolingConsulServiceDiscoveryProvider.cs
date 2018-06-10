using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Consul;
using Ocelot.Infrastructure.Consul;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Logging;
using Ocelot.ServiceDiscovery.Configuration;
using Ocelot.Values;

namespace Ocelot.ServiceDiscovery.Providers
{
    public class PollingConsulServiceDiscoveryProvider : IServiceDiscoveryProvider
    {
        private readonly PollingConsulRegistryConfiguration _config;
        private readonly IOcelotLogger _logger;
        private readonly IConsulClient _consul;
        private readonly Timer _timer;
        private const string VersionPrefix = "version-";
        private bool _polling;
        private SemaphoreSlim _semaphore = new SemaphoreSlim(1,1);
        private ConcurrentBag<Service> _services;

        public PollingConsulServiceDiscoveryProvider(PollingConsulRegistryConfiguration config, IOcelotLoggerFactory factory, IConsulClientFactory clientFactory)
        {;
            _logger = factory.CreateLogger<PollingConsulServiceDiscoveryProvider>();

            _config = config;
            _consul = clientFactory.Get(_config);
            _services = new ConcurrentBag<Service>();
        
            _timer = new Timer(async x =>
            {
                if(_polling)
                {
                    return;
                }
                
                _polling = true;
                await Poll();
                _polling = false;
            }, null, _config.Delay, _config.Delay);
        }

        public Task<List<Service>> Get()
        {
            return Task.FromResult(_services.ToList());
        }

        private async Task Poll()
        {
            try
            {
                var queryResult = await _consul.Health.Service(_config.KeyOfServiceInConsul, string.Empty, true);

                var services = new ConcurrentBag<Service>();

                foreach (var serviceEntry in queryResult.Response)
                {
                    if (IsValid(serviceEntry))
                    {
                        services.Add(BuildService(serviceEntry));
                    }
                    else
                    {
                        _logger.LogWarning($"Unable to use service Address: {serviceEntry.Service.Address} and Port: {serviceEntry.Service.Port} as it is invalid. Address must contain host only e.g. localhost and port must be greater than 0");
                    }
                }

                _services = services;
            }
            catch (Exception e)
            {
                _logger.LogError($"Unable to retrieve latest services while polling for {_config.KeyOfServiceInConsul}, returning stale data", e);
            }
            finally
            {
                await _semaphore.WaitAsync();
            }
        }

        private Service BuildService(ServiceEntry serviceEntry)
        {
            return new Service(
                serviceEntry.Service.Service,
                new ServiceHostAndPort(serviceEntry.Service.Address, serviceEntry.Service.Port),
                serviceEntry.Service.ID,
                GetVersionFromStrings(serviceEntry.Service.Tags),
                serviceEntry.Service.Tags ?? Enumerable.Empty<string>());
        }

        private bool IsValid(ServiceEntry serviceEntry)
        {
            if (serviceEntry.Service.Address.Contains("http://") || serviceEntry.Service.Address.Contains("https://") || serviceEntry.Service.Port <= 0)
            {
                return false;
            }

            return true;
        }

        private string GetVersionFromStrings(IEnumerable<string> strings)
        {
            return strings
                ?.FirstOrDefault(x => x.StartsWith(VersionPrefix, StringComparison.Ordinal))
                .TrimStart(VersionPrefix);
        }
    }
}
