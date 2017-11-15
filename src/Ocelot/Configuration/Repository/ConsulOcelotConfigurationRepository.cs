using System;
using System.Text;
using System.Threading.Tasks;
using Consul;
using Newtonsoft.Json;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery;

namespace Ocelot.Configuration.Repository
{
    public class ConsulOcelotConfigurationRepository : IOcelotConfigurationRepository
    {
        private readonly ConsulClient _consul;
        private string _ocelotConfiguration = "OcelotConfiguration";
        private readonly Cache.IOcelotCache<IOcelotConfiguration> _cache;


        public ConsulOcelotConfigurationRepository(Cache.IOcelotCache<IOcelotConfiguration> cache, ServiceProviderConfiguration serviceProviderConfig)
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

        public async Task<Response<IOcelotConfiguration>> Get()
        {
            var config = _cache.Get(_ocelotConfiguration, _ocelotConfiguration);

            if (config != null)
            {
                return new OkResponse<IOcelotConfiguration>(config);
            }

            var queryResult = await _consul.KV.Get(_ocelotConfiguration);

            if (queryResult.Response == null)
            {
                return new OkResponse<IOcelotConfiguration>(null);
            }

            var bytes = queryResult.Response.Value;

            var json = Encoding.UTF8.GetString(bytes);

            var consulConfig = JsonConvert.DeserializeObject<OcelotConfiguration>(json);

            return new OkResponse<IOcelotConfiguration>(consulConfig);
        }

        public async Task<Response> AddOrReplace(IOcelotConfiguration ocelotConfiguration)
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

            return new ErrorResponse(new UnableToSetConfigInConsulError("Unable to set config in consul"));
        }
    }
}