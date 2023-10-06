using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ocelot.Configuration.Repository;
using Ocelot.DependencyInjection;

namespace Ocelot.Provider.Consul;

public static class OcelotBuilderExtensions
{
    public static IOcelotBuilder AddConsul(this IOcelotBuilder builder)
    {
        builder.Services
            .AddSingleton(ConsulProviderFactory.Get)
            .AddSingleton<IConsulClientFactory, ConsulClientFactory>()
            .RemoveAll(typeof(IFileConfigurationPollerOptions))
            .AddSingleton<IFileConfigurationPollerOptions, ConsulFileConfigurationPollerOption>();
        return builder;
    }

    public static IOcelotBuilder AddConfigStoredInConsul(this IOcelotBuilder builder)
    {
        builder.Services
            .AddSingleton(ConsulMiddlewareConfigurationProvider.Get)
            .AddHostedService<FileConfigurationPoller>()
            .AddSingleton<IFileConfigurationRepository, ConsulFileConfigurationRepository>();
        return builder;
    }
}
