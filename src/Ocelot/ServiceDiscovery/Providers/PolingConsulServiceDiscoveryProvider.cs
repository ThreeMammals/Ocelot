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
        private readonly ConsulRegistryConfiguration _config;
        private readonly IOcelotLogger _logger;
        private readonly IConsulClient _consul;
        private const string VersionPrefix = "version-";
        private List<Service> _services;

        private int _pollingInterval;
        private ulong _waitIndex;

        public PollingConsulServiceDiscoveryProvider(int pollingInterval, ConsulRegistryConfiguration config, IOcelotLoggerFactory factory, IConsulClientFactory clientFactory)
        {
            ;
            _pollingInterval = pollingInterval == 0 ? 10000 : pollingInterval;
            _logger = factory.CreateLogger<PollingConsulServiceDiscoveryProvider>();

            _config = config;
            _consul = clientFactory.Get(_config);

            Task.Factory.StartNew(async () =>
            {
                await Poll();
            });
        }

        public async Task<List<Service>> Get()
        {
            if (_services == null)
            {
                _services = await GetService();
            }

            return _services;
        }

        public async Task<List<Service>> GetService()
        {
            QueryResult<ServiceEntry[]> queryResult = null;
            List<Service> services = new List<Service>();

            if (_waitIndex == 0 || _pollingInterval == 0)
            {
                queryResult = await _consul.Health.Service(_config.KeyOfServiceInConsul, string.Empty, true);
            }
            else
            {
                // block queries
                queryResult = await _consul.Health.Service(_config.KeyOfServiceInConsul, string.Empty, true,
                    new QueryOptions() { WaitIndex = _waitIndex, WaitTime = TimeSpan.FromMilliseconds(_pollingInterval) });
            }

            _waitIndex = queryResult.LastIndex; // store waitIndex

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

            return services.ToList();
        }

        private async Task Poll()
        {
            _logger.LogInformation("Started polling services from consul");

            _services = await GetService();

            _logger.LogInformation("Finished polling services from consul");

            await Poll();
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
