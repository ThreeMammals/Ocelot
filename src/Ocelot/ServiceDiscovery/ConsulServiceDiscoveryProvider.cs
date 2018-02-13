using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Consul;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Logging;
using Ocelot.Values;

namespace Ocelot.ServiceDiscovery
{
    public class ConsulServiceDiscoveryProvider : IServiceDiscoveryProvider
    {
        private readonly ConsulRegistryConfiguration _consulConfig;
        private readonly IOcelotLogger _logger;
        private readonly ConsulClient _consul;
        private const string VersionPrefix = "version-";

        public ConsulServiceDiscoveryProvider(ConsulRegistryConfiguration consulRegistryConfiguration, IOcelotLoggerFactory factory)
        {;
            _logger = factory.CreateLogger<ConsulServiceDiscoveryProvider>();

            var consulHost = string.IsNullOrEmpty(consulRegistryConfiguration?.HostName) ? "localhost" : consulRegistryConfiguration.HostName;

            var consulPort = consulRegistryConfiguration?.Port ?? 8500;

            _consulConfig = new ConsulRegistryConfiguration(consulHost, consulPort, consulRegistryConfiguration?.KeyOfServiceInConsul);

            _consul = new ConsulClient(config =>
            {
                config.Address = new Uri($"http://{_consulConfig.HostName}:{_consulConfig.Port}");
            });
        }

        public async Task<List<Service>> Get()
        {
            var queryResult = await _consul.Health.Service(_consulConfig.KeyOfServiceInConsul, string.Empty, true);

            var services = new List<Service>();

            foreach (var serviceEntry in queryResult.Response)
            {
                if (IsValid(serviceEntry))
                {
                    services.Add(BuildService(serviceEntry));
                }
                else
                {
                    _logger.LogError($"Unable to use service Address: {serviceEntry.Service.Address} and Port: {serviceEntry.Service.Port} as it is invalid. Address must contain host only e.g. localhost and port must be greater than 0");
                }
            }

            return services.ToList();
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
