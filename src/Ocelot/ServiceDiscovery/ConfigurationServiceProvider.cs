using System.Collections.Generic;
using System.Threading.Tasks;
using Ocelot.Values;
    
namespace Ocelot.ServiceDiscovery
{
    public class ConfigurationServiceProvider : IServiceDiscoveryProvider
    {
        private readonly List<Service> _services;

        public ConfigurationServiceProvider(List<Service> services)
        {
            _services = services;
        }

        public async Task<List<Service>> Get()
        {
            return await Task.FromResult(_services);
        }
    }
}