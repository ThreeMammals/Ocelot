using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ocelot.DependencyInjection;
using Ocelot.DownstreamUrlCreator;
using Ocelot.ServiceDiscovery;
using Ocelot.ServiceDiscovery.Configuration;
using Ocelot.ServiceDiscovery.Providers;

namespace Ocelot.ServiceFabric;

public static class ServiceFabricServiceCollectionExtensions
{
    public static IOcelotBuilder AddServiceFabric(this IOcelotBuilder builder)
    {
        builder.Services.AddSingleton<ServiceDiscoveryFinderDelegate>((_, config, route) =>
        {
            if (!ServiceFabricServiceDiscoveryProvider.Type.Equals(config.Type, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var serviceFabricConfiguration = new ServiceFabricConfiguration(config.Host, config.Port, route.ServiceName);
            return new ServiceFabricServiceDiscoveryProvider(serviceFabricConfiguration);
        });

        builder.Services.Replace(ServiceDescriptor.Singleton<IDownstreamPathFormatter, ServiceFabricDownstreamPathFormatter>());
        return builder;
    }
}
