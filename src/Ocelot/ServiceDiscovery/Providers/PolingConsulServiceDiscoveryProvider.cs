using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Consul;
using Ocelot.Infrastructure.Consul;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Logging;
using Ocelot.ServiceDiscovery.Configuration;
using Ocelot.Values;

namespace Ocelot.ServiceDiscovery.Providers
{
    public class PollingConsulServiceDiscoveryProvider : IServiceDiscoveryProvider
    {
        private readonly IOcelotLogger _logger;
        private readonly IServiceDiscoveryProvider _consulServiceDiscoveryProvider;
        private readonly Timer _timer;
        private bool _polling;
        private List<Service> _services;
        private string _keyOfServiceInConsul;

        public PollingConsulServiceDiscoveryProvider(int pollingInterval, string keyOfServiceInConsul, IOcelotLoggerFactory factory, IServiceDiscoveryProvider consulServiceDiscoveryProvider)
        {;
            _logger = factory.CreateLogger<PollingConsulServiceDiscoveryProvider>();
            _keyOfServiceInConsul = keyOfServiceInConsul;
            _consulServiceDiscoveryProvider = consulServiceDiscoveryProvider;
            _services = new List<Service>();
        
            _timer = new Timer(async x =>
            {
                    if(_polling)
                    {
                        return;
                    }
                    
                    _polling = true;
                    await Poll();
                    _polling = false;
                
            }, null, pollingInterval, pollingInterval);
        }

        public Task<List<Service>> Get()
        {   
            return Task.FromResult(_services);
        }

        private async Task Poll()
        {
            _services = await _consulServiceDiscoveryProvider.Get();
        }
    }
}
