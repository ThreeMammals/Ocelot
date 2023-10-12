using Ocelot.Values;

namespace Ocelot.ServiceDiscovery.Providers
{
    public class ConfigurationServiceProvider : IServiceDiscoveryProvider
    {
        private readonly List<Service> _services;

        public ConfigurationServiceProvider(List<Service> services)
        {
            _services = services;
        }

        public async Task<List<Service>> GetAsync()
        {
            return await Task.FromResult(_services);
        }
    }
}
