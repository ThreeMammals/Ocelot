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
        private readonly ConsulRegistryConfiguration _consulConfig;
        private readonly ConsulClient _consul;
        private const string VersionPrefix = "version-";

        public ConsulServiceDiscoveryProvider(ConsulRegistryConfiguration consulRegistryConfiguration)
        {
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
