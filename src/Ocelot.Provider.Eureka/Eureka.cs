using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;

namespace Ocelot.Provider.Eureka
{
    public class Eureka : IServiceDiscoveryProvider
    {
        private readonly string _serviceName;
        private readonly IDiscoveryClient _client;

        public Eureka(string serviceName, IDiscoveryClient client)
        {
            _serviceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public Task<List<Service>> GetAsync()
        {
            var services = new List<Service>();

            var instances = _client.GetInstances(_serviceName);
            if (instances != null && instances.Any())
            {
                services.AddRange(instances.Select(i => new Service(i.ServiceId, new ServiceHostAndPort(i.Host, i.Port, i.Uri.Scheme), string.Empty, string.Empty, new List<string>())));
            }

            return Task.FromResult(services);
        }
    }
}
