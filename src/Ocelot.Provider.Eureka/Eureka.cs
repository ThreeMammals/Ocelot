﻿namespace Ocelot.Provider.Eureka
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using ServiceDiscovery.Providers;
    using Steeltoe.Common.Discovery;
    using Values;

    public class Eureka : IServiceDiscoveryProvider
    {
        private readonly IDiscoveryClient _client;
        private readonly string _serviceName;

        public Eureka(string serviceName, IDiscoveryClient client)
        {
            _client = client;
            _serviceName = serviceName;
        }

        public Task<List<Service>> Get()
        {
            var services = new List<Service>();

            var instances = _client.GetInstances(_serviceName);

            if (instances != null && instances.Any())
            {
                services.AddRange(instances.Select(i => new Service(i.ServiceId, new ServiceHostAndPort(i.Host, i.Port), "", "", new List<string>())));
            }

            return Task.FromResult(services);
        }
    }
}
