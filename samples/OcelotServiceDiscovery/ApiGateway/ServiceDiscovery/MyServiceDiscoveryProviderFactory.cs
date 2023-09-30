using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery;
using Ocelot.ServiceDiscovery.Providers;
using System;

namespace Ocelot.Samples.ServiceDiscovery.ApiGateway.ServiceDiscovery;

public class MyServiceDiscoveryProviderFactory : IServiceDiscoveryProviderFactory
{
    private readonly IOcelotLoggerFactory _factory;
    private readonly IServiceProvider _provider;

    public MyServiceDiscoveryProviderFactory(IOcelotLoggerFactory factory, IServiceProvider provider)
    {
        _factory = factory;
        _provider = provider;
    }

    public Response<IServiceDiscoveryProvider> Get(ServiceProviderConfiguration serviceConfig, DownstreamRoute route)
    {
        // Apply configuration checks
        // ...

        // Create the provider based on configuration and route info
        var provider = new MyServiceDiscoveryProvider(_provider, serviceConfig, route);

        return new OkResponse<IServiceDiscoveryProvider>(provider);
    }
}
