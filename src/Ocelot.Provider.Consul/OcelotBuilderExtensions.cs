﻿namespace Ocelot.Provider.Consul
{
    using Configuration.Repository;
    using DependencyInjection;
    using Microsoft.Extensions.DependencyInjection;
    using Middleware;
    using ServiceDiscovery;

    public static class OcelotBuilderExtensions
    {
        public static IOcelotBuilder AddConsul(this IOcelotBuilder builder)
        {
            builder.Services.AddSingleton<ServiceDiscoveryFinderDelegate>(ConsulProviderFactory.Get);
            builder.Services.AddSingleton<IConsulClientFactory, ConsulClientFactory>();
            return builder;
        }

        public static IOcelotBuilder AddConfigStoredInConsul(this IOcelotBuilder builder)
        {
            builder.Services.AddSingleton<OcelotMiddlewareConfigurationDelegate>(ConsulMiddlewareConfigurationProvider.Get);
            builder.Services.AddHostedService<FileConfigurationPoller>();
            builder.Services.AddSingleton<IFileConfigurationRepository, ConsulFileConfigurationRepository>();
            return builder;
        }
    }
}
