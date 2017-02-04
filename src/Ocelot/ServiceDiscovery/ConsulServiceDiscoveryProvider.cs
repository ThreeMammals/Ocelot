using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Consul;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Values;

namespace Ocelot.ServiceDiscovery
{
    public class ConsulServiceDiscoveryProvider : IServiceDiscoveryProvider
    {
        private readonly ConsulRegistryConfiguration _configuration;
        private readonly ConsulClient _consul;
        private const string VersionPrefix = "version-";

        public ConsulServiceDiscoveryProvider(ConsulRegistryConfiguration consulRegistryConfiguration)
        {
            var consulHost = string.IsNullOrEmpty(consulRegistryConfiguration?.HostName) ? "localhost" : consulRegistryConfiguration.HostName;
            var consulPort = consulRegistryConfiguration?.Port ?? 8500;
            _configuration = new ConsulRegistryConfiguration(consulHost, consulPort, consulRegistryConfiguration?.ServiceName);

            _consul = new ConsulClient(config =>
            {
                config.Address = new Uri($"http://{_configuration.HostName}:{_configuration.Port}");
            });
        }

        public async Task<List<Service>> Get()
        {
            var queryResult = await _consul.Health.Service(_configuration.ServiceName, string.Empty, true);

            var services = queryResult.Response.Select(BuildService);

            return services.ToList();
        }

        private Service BuildService(ServiceEntry serviceEntry)
        {
            return new Service(
                serviceEntry.Service.Service,
                new HostAndPort(serviceEntry.Service.Address, serviceEntry.Service.Port),
                serviceEntry.Service.ID,
                GetVersionFromStrings(serviceEntry.Service.Tags),
                serviceEntry.Service.Tags ?? Enumerable.Empty<string>());
        }

        private string GetVersionFromStrings(IEnumerable<string> strings)
        {
            return strings
                ?.FirstOrDefault(x => x.StartsWith(VersionPrefix, StringComparison.Ordinal))
                .TrimStart(VersionPrefix);
        }
    }
}
