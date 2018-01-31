using System;
using System.Text;
using System.Threading.Tasks;
using Consul;
using Newtonsoft.Json;
using Ocelot.Configuration.File;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery;

namespace Ocelot.Configuration.Repository
{

    public class ConsulFileConfigurationRepository : IFileConfigurationRepository
    {
        private readonly ConsulClient _consul;
        private string _ocelotConfiguration = "OcelotConfiguration";
        private readonly Cache.IOcelotCache<FileConfiguration> _cache;

        public ConsulFileConfigurationRepository(Cache.IOcelotCache<FileConfiguration> cache, ServiceProviderConfiguration serviceProviderConfig)
        {
            var consulHost = string.IsNullOrEmpty(serviceProviderConfig?.ServiceProviderHost) ? "localhost" : serviceProviderConfig?.ServiceProviderHost;
            var consulPort = serviceProviderConfig?.ServiceProviderPort ?? 8500;
            var configuration = new ConsulRegistryConfiguration(consulHost, consulPort, _ocelotConfiguration);
            _cache = cache;
            _consul = new ConsulClient(c =>
            {
                c.Address = new Uri($"http://{configuration.HostName}:{configuration.Port}");
            });
        }

        public async Task<Response<FileConfiguration>> Get()
        {
            var config = _cache.Get(_ocelotConfiguration, _ocelotConfiguration);

            if (config != null)
            {
                return new OkResponse<FileConfiguration>(config);
            }

            var queryResult = await _consul.KV.Get(_ocelotConfiguration);

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

            var kvPair = new KVPair(_ocelotConfiguration)
            {
                Value = bytes
            };

            var result = await _consul.KV.Put(kvPair);

            if (result.Response)
            {
                _cache.AddAndDelete(_ocelotConfiguration, ocelotConfiguration, TimeSpan.FromSeconds(3), _ocelotConfiguration);

                return new OkResponse();
            }

            return new ErrorResponse(new UnableToSetConfigInConsulError($"Unable to set FileConfiguration in consul, response status code from consul was {result.StatusCode}"));
        }
    }
}