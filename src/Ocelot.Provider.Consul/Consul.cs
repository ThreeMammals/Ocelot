﻿using Consul;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Logging;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;

namespace Ocelot.Provider.Consul
{
    public class Consul : IServiceDiscoveryProvider
    {
        private readonly ConsulRegistryConfiguration _config;
        private readonly IOcelotLogger _logger;
        private readonly IConsulClient _consul;
        private const string VersionPrefix = "version-";

        public Consul(ConsulRegistryConfiguration config, IOcelotLoggerFactory factory, IConsulClientFactory clientFactory)
        {
            _config = config;
            _logger = factory.CreateLogger<Consul>();
            _consul = clientFactory.Get(_config);
        }

        public async Task<List<Service>> Get()
        {
            var consulAddress = (_consul as ConsulClient)?.Config.Address;
            _logger.LogDebug($"Querying Consul {consulAddress} about a service: {_config.KeyOfServiceInConsul}");

            var queryResult = await _consul.Health.Service(_config.KeyOfServiceInConsul, string.Empty, true);

            var services = new List<Service>();

            foreach (var serviceEntry in queryResult.Response)
            {
                var address = serviceEntry.Service.Address;
                var port = serviceEntry.Service.Port;

                if (IsValid(serviceEntry))
                {
                    var nodes = await _consul.Catalog.Nodes();
                    if (nodes.Response == null)
                    {
                        services.Add(BuildService(serviceEntry, null));
                    }
                    else
                    {
                        var serviceNode = nodes.Response.FirstOrDefault(n => n.Address == address);
                        services.Add(BuildService(serviceEntry, serviceNode));
                    }

                    _logger.LogDebug($"Consul answer: Address: {address}, Port: {port}");
                }
                else
                {
                    _logger.LogWarning($"Unable to use service Address: {address} and Port: {port} as it is invalid. Address must contain host only e.g. localhost and port must be greater than 0");
                }
            }

            return services.ToList();
        }

        private static Service BuildService(ServiceEntry serviceEntry, Node serviceNode)
        {
            return new Service(
                serviceEntry.Service.Service,
                new ServiceHostAndPort(serviceNode == null ? serviceEntry.Service.Address : serviceNode.Name, serviceEntry.Service.Port),
                serviceEntry.Service.ID,
                GetVersionFromStrings(serviceEntry.Service.Tags),
                serviceEntry.Service.Tags ?? Enumerable.Empty<string>());
        }

        private static bool IsValid(ServiceEntry serviceEntry)
        {
            if (string.IsNullOrEmpty(serviceEntry.Service.Address) || serviceEntry.Service.Address.Contains("http://") || serviceEntry.Service.Address.Contains("https://") || serviceEntry.Service.Port <= 0)
            {
                return false;
            }

            return true;
        }

        private static string GetVersionFromStrings(IEnumerable<string> strings)
            => strings?
                .FirstOrDefault(x => x.StartsWith(VersionPrefix, StringComparison.Ordinal))
                .TrimStart(VersionPrefix);
    }
}
