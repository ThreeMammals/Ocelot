using System.Collections.Generic;
using Ocelot.Values;

namespace Ocelot.ServiceDiscovery
{
    public class ConfigurationServiceProvider : IServiceProvider
    {
        private List<Service> _services;

        public ConfigurationServiceProvider(List<Service> services)
        {
            _services = services;
        }

        public List<Service> Get()
        {
            return _services;
        }
    }
}