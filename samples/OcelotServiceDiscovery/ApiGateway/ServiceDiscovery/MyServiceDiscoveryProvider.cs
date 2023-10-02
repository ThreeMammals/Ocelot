using Ocelot.Configuration;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ocelot.Samples.ServiceDiscovery.ApiGateway.ServiceDiscovery;

public class MyServiceDiscoveryProvider : IServiceDiscoveryProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ServiceProviderConfiguration _config;
    private readonly DownstreamRoute _downstreamRoute;

    public MyServiceDiscoveryProvider(IServiceProvider serviceProvider, ServiceProviderConfiguration config, DownstreamRoute downstreamRoute)
    {
        _serviceProvider = serviceProvider;
        _config = config;
        _downstreamRoute = downstreamRoute;
    }

    public Task<List<Service>> GetAsync()
    {

        // Returns a list of service(s) that match the downstream route passed to the provider
        var services = new List<Service>();

        // Apply configuration checks
        // ... if (_config.Host)
        if (_downstreamRoute.ServiceName.Equals("downstream-service"))
        {
            //For this example we simply do a manual match to a single service
            var service = new Service(
                name: "downstream-service",
                hostAndPort: new ServiceHostAndPort("localhost", 5001),
                id: "downstream-service-1",
                version: "1.0",
                tags: new string[] { "downstream", "hardcoded" }
            );

            services.Add(service);
        }

        return Task.FromResult(services);
    }
}
