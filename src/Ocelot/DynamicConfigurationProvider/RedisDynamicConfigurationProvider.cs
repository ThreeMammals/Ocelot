using Newtonsoft.Json;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.Infrastructure.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;
using Ocelot.Logging;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.Parser;

namespace Ocelot.DynamicConfigurationProvider
{
    [ConfigurationStore("Redis")]
    public class RedisDynamicConfigurationProvider : DynamicConfigurationProvider
    {
        private readonly IRedisClientFactory _factory;
        private readonly IRedisDataConfigurationParser _configurationParser;

        public RedisDynamicConfigurationProvider(IServiceProvider provider,
            IRedisDataConfigurationParser configurationParser, IOcelotLoggerFactory loggerFactory)
            :base(loggerFactory.CreateLogger<RedisDynamicConfigurationProvider>())
        {
            _factory = provider.GetService(typeof(IRedisClientFactory)) as IRedisClientFactory;
            _configurationParser = configurationParser;
        }

        protected override async Task<FileReRoute> GetRouteConfigurationAsync(string host, string port, string key)
        {
            _logger.LogInformation($"Getting route configuration from Redis store at {host}:{port} for {key}");

            try
            {
                //TODO: there should be way to pass constructr parameters while resolving types using IServiceProvider, then host/port can be passed as constuctor paramter
                var db = _factory.Get(host, port);
                var hashEntries = await db.HashGetAllAsync(key);

                _logger.LogInformation($"Found {hashEntries.Length} hash entries from Redis store, including keys.");
                return _configurationParser.Parse(hashEntries.ToList());
            }
            catch (RedisConnectionException exception)
            {
                _logger.LogError($"Unable to connect to Redis server {host}:{port}.", exception);
                throw exception;
            }
        }
    }
}
