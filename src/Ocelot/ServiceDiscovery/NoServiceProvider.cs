using System.Collections.Generic;
using Ocelot.Values;

namespace Ocelot.ServiceDiscovery
{
    public class NoServiceProvider : IServiceProvider
    {
        private List<Service> _services;

        public NoServiceProvider(List<Service> services)
        {
            _services = services;
        }

        public List<Service> Get()
        {
            return _services;
        }
    }
}