using Nacos.V2;
using Nacos.V2.Exceptions;
using Nacos.V2.Naming.Dtos;
using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;
using System.Net;
using Service = Ocelot.Values.Service;

namespace Ocelot.Provider.Nacos
{
    public class Nacos : IServiceDiscoveryProvider
    {
        private readonly INacosNamingService _client;
        private readonly string _serviceName;

        public Nacos(string serviceName,INacosNamingService client)
        {
            _client = client;
            _serviceName = serviceName;
        }

        public async Task<List<Service>> GetAsync()
        {
            try
            {
                var instances = await _client.GetAllInstances(_serviceName)
                    .ConfigureAwait(false);

                return instances?
                    .Where(i => i.Healthy && i.Enabled && i.Weight > 0) // 健康检查过滤
                    .Select(TransformInstance)
                    .ToList() ?? new List<Service>();
            }
            catch (NacosException ex)
            {
                throw ex;
            }
        }

        private Service TransformInstance(Instance instance)
        {
            var metadata = instance.Metadata ?? new Dictionary<string, string>();

            return new Service(
                id: instance.InstanceId,
                hostAndPort: new ServiceHostAndPort(instance.Ip, instance.Port),
                name: instance.ServiceName,
                version: metadata.GetValueOrDefault("version", "default"),
                tags: ProcessMetadataTags(metadata)
            );
        }

        private List<string> ProcessMetadataTags(IDictionary<string, string> metadata)
        {
            return metadata
                .Where(kv => !_reservedKeys.Contains(kv.Key))
                .Select(kv => FormatTag(kv))
                .ToList();
        }

        private string FormatTag(KeyValuePair<string, string> kv)
        {
            var encodedKey = WebUtility.UrlEncode(kv.Key);
            var encodedValue = WebUtility.UrlEncode(kv.Value);
            return $"{encodedKey}={encodedValue}";
        }

        private static readonly string[] _reservedKeys = { "version", "group", "cluster", "namespace", "weight" };
    }
}
