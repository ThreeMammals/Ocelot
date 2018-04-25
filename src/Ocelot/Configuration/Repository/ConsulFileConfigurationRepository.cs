namespace Ocelot.Configuration.Repository
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using Consul;
    using Newtonsoft.Json;
    using Ocelot.Configuration.File;
    using Ocelot.Infrastructure.Consul;
    using Ocelot.Logging;
    using Ocelot.Responses;
    using Ocelot.ServiceDiscovery.Configuration;

    public class ConsulFileConfigurationRepository : IFileConfigurationRepository
    {
        private readonly IConsulClient _consul;
        private const string OcelotConfiguration = "InternalConfiguration";
        private readonly Cache.IOcelotCache<FileConfiguration> _cache;
        private readonly IOcelotLogger _logger;

        public ConsulFileConfigurationRepository(
            Cache.IOcelotCache<FileConfiguration> cache,
            IInternalConfigurationRepository repo, 
            IConsulClientFactory factory,
            IOcelotLoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ConsulFileConfigurationRepository>();
            _cache = cache;

            var internalConfig = repo.Get();

            var consulHost = "localhost";
            var consulPort = 8500;
            string token = null;

            if (!internalConfig.IsError)
            {
                consulHost = string.IsNullOrEmpty(internalConfig.Data.ServiceProviderConfiguration?.Host) ? consulHost : internalConfig.Data.ServiceProviderConfiguration?.Host;
                consulPort = internalConfig.Data.ServiceProviderConfiguration?.Port ?? consulPort;
                token = internalConfig.Data.ServiceProviderConfiguration?.Token;
            }

            var config = new ConsulRegistryConfiguration(consulHost, consulPort, OcelotConfiguration, token);

            _consul = factory.Get(config);
        }

        public async Task<Response<FileConfiguration>> Get()
        {
            var config = _cache.Get(OcelotConfiguration, OcelotConfiguration);

            if (config != null)
            {
                return new OkResponse<FileConfiguration>(config);
            }

            var queryResult = await _consul.KV.Get(OcelotConfiguration);

            if (queryResult.Response == null)
            {
                return new OkResponse<FileConfiguration>(null);
            }

            var bytes = queryResult.Response.Value;

            var json = Encoding.UTF8.GetString(bytes);

            var consulConfig = JsonConvert.DeserializeObject<FileConfiguration>(json);

            return new OkResponse<FileConfiguration>(consulConfig);
        }

        public async Task<Response> Set(FileConfiguration ocelotConfiguration)
        {
            var json = JsonConvert.SerializeObject(ocelotConfiguration, Formatting.Indented);

            var bytes = Encoding.UTF8.GetBytes(json);

            var kvPair = new KVPair(OcelotConfiguration)
            {
                Value = bytes
            };

            var result = await _consul.KV.Put(kvPair);

            if (result.Response)
            {
                _cache.AddAndDelete(OcelotConfiguration, ocelotConfiguration, TimeSpan.FromSeconds(3), OcelotConfiguration);

                return new OkResponse();
            }

            return new ErrorResponse(new UnableToSetConfigInConsulError($"Unable to set FileConfiguration in consul, response status code from consul was {result.StatusCode}"));
        }
    }
}
