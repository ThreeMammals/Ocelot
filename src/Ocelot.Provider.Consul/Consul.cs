namespace Ocelot.Provider.Consul
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using global::Consul;
    using Infrastructure.Extensions;
    using Logging;
    using ServiceDiscovery.Providers;
    using Values;


    public class Consul : IServiceDiscoveryProvider
    {
        private readonly ConsulRegistryConfiguration _config;
        private readonly IOcelotLogger _logger;
        private readonly IConsulClient _consul;
        private const string VersionPrefix = "version-";

        public Consul(ConsulRegistryConfiguration config, IOcelotLoggerFactory factory, IConsulClientFactory clientFactory)
        {
            _logger = factory.CreateLogger<Consul>();
            _config = config;
            _consul = clientFactory.Get(_config);
        }

        public async Task<List<Service>> Get()
        {
            var queryResult = await _consul.Catalog.Service(_config.KeyOfServiceInConsul, string.Empty);

            var services = new List<Service>();

            foreach (var serviceEntry in queryResult.Response)
            {
                if (IsValid(serviceEntry))
                {
                    services.Add(BuildService(serviceEntry));
                }
                else
                {
                    _logger.LogWarning($"Unable to use service Address: {serviceEntry.Address} and Port: {serviceEntry.ServicePort} as it is invalid. Address must contain host only e.g. localhost and port must be greater than 0");
                }
            }

            return services.ToList();
        }

        private Service BuildService(CatalogService serviceEntry)
        {
            return new Service(
                serviceEntry.ServiceName,
                new ServiceHostAndPort(serviceEntry.Address, serviceEntry.ServicePort),
                serviceEntry.ServiceID,
                GetVersionFromStrings(serviceEntry.ServiceTags),
                serviceEntry.ServiceTags ?? Enumerable.Empty<string>());
        }

        private bool IsValid(CatalogService serviceEntry)
        {
            if (string.IsNullOrEmpty(serviceEntry.Address) || serviceEntry.Address.Contains("http://") || serviceEntry.Address.Contains("https://") || serviceEntry.ServicePort <= 0)
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
