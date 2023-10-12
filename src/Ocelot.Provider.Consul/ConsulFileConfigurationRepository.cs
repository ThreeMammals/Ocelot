using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Ocelot.Cache;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.Logging;
using Ocelot.Responses;
using System.Text;

namespace Ocelot.Provider.Consul;

public class ConsulFileConfigurationRepository : IFileConfigurationRepository
{
    private readonly IOcelotCache<FileConfiguration> _cache;
    private readonly string _configurationKey;
    private readonly IConsulClient _consul;
    private readonly IOcelotLogger _logger;

    public ConsulFileConfigurationRepository(
        IOptions<FileConfiguration> fileConfiguration,
        IOcelotCache<FileConfiguration> cache,
        IConsulClientFactory factory,
        IOcelotLoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ConsulFileConfigurationRepository>();
        _cache = cache;

        var provider = fileConfiguration.Value.GlobalConfiguration.ServiceDiscoveryProvider;
        _configurationKey = string.IsNullOrWhiteSpace(provider.ConfigurationKey)
            ? nameof(InternalConfiguration)
            : provider.ConfigurationKey;

        var config = new ConsulRegistryConfiguration(provider.Scheme, provider.Host,
            provider.Port, _configurationKey, provider.Token);
        _consul = factory.Get(config);
    }

    public async Task<Response<FileConfiguration>> Get()
    {
        var config = _cache.Get(_configurationKey, _configurationKey);
        if (config != null)
        {
            return new OkResponse<FileConfiguration>(config);
        }

        var queryResult = await _consul.KV.Get(_configurationKey);
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
        var kvPair = new KVPair(_configurationKey)
        {
            Value = bytes,
        };

        var result = await _consul.KV.Put(kvPair);
        if (result.Response)
        {
            _cache.AddAndDelete(_configurationKey, ocelotConfiguration, TimeSpan.FromSeconds(3), _configurationKey);

            return new OkResponse();
        }

        return new ErrorResponse(new UnableToSetConfigInConsulError(
            $"Unable to set {nameof(FileConfiguration)} in {nameof(Consul)}, response status code from {nameof(Consul)} was {result.StatusCode}"));
    }
}
