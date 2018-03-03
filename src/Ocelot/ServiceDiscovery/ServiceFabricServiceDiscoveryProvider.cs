using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ocelot.Values;

namespace Ocelot.ServiceDiscovery
{
    public class ServiceFabricServiceDiscoveryProvider : IServiceDiscoveryProvider
    {
        private readonly ServiceFabricConfiguration _configuration;

        public ServiceFabricServiceDiscoveryProvider(ServiceFabricConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<List<Service>> Get()
        {
            return new List<Service>
            {
                new Service(_configuration.ServiceName, 
                    new ServiceHostAndPort(_configuration.HostName, _configuration.Port), 
                    "doesnt matter with service fabric", 
                    "doesnt matter with service fabric", 
                    new List<string>())
            };
        }
    }
}
