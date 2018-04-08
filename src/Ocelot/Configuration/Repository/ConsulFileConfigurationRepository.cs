using System;
using System.Text;
using System.Threading.Tasks;
using Consul;
using Newtonsoft.Json;
using Ocelot.Configuration.File;
using Ocelot.Infrastructure.Consul;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery.Configuration;

namespace Ocelot.Configuration.Repository
{
    public class ConsulFileConfigurationRepository : IFileConfigurationRepository
    {
        private readonly ConsulClient _consul;
        private const string OcelotConfiguration = "OcelotConfiguration";
        private readonly Cache.IOcelotCache<FileConfiguration> _cache;

        public ConsulFileConfigurationRepository(
            Cache.IOcelotCache<FileConfiguration> cache, 
            ServiceProviderConfiguration serviceProviderConfig, 
            IConsulClientFactory factory)
        {
            var consulHost = string.IsNullOrEmpty(serviceProviderConfig?.Host) ? "localhost" : serviceProviderConfig?.Host;
            var consulPort = serviceProviderConfig?.Port ?? 8500;
            var config = new ConsulRegistryConfiguration(consulHost, consulPort, OcelotConfiguration, serviceProviderConfig?.Token);
            _cache = cache;
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
            var json = JsonConvert.SerializeObject(ocelotConfiguration);

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
