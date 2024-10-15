using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ocelot.Configuration.Repository;
using Ocelot.DependencyInjection;
using Ocelot.Provider.Consul.Interfaces;

namespace Ocelot.Provider.Consul;

public static class OcelotBuilderExtensions
{
    /// <summary>
    /// Integrates Consul service discovery into the DI, atop the existing Ocelot services.
    /// </summary>
    /// <remarks>
    /// Default services:
    /// <list type="bullet">
    /// <item>The <see cref="IConsulClientFactory"/> service is an instance of <see cref="ConsulClientFactory"/>.</item>
    /// <item>The <see cref="IConsulServiceBuilder"/> service is an instance of <see cref="DefaultConsulServiceBuilder"/>.</item>
    /// </list>
    /// </remarks>
    /// <param name="builder">The Ocelot Builder instance, default.</param>
    /// <returns>The reference to the same extended <see cref="IOcelotBuilder"/> object.</returns>
    public static IOcelotBuilder AddConsul(this IOcelotBuilder builder)
    {
        builder.Services
            .AddSingleton(ConsulProviderFactory.Get)
            .AddSingleton<IConsulClientFactory, ConsulClientFactory>()
            .AddScoped<IConsulServiceBuilder, DefaultConsulServiceBuilder>()
            .RemoveAll(typeof(IFileConfigurationPollerOptions))
            .AddSingleton<IFileConfigurationPollerOptions, ConsulFileConfigurationPollerOption>();
        return builder;
    }

    /// <summary>
    /// Integrates Consul service discovery into the DI, atop the existing Ocelot services, with service builder overriding.
    /// </summary>
    /// <remarks>
    /// Services to override:
    /// <list type="bullet">
    /// <item>The <see cref="IConsulServiceBuilder"/> service has been substituted with a <typeparamref name="TServiceBuilder"/> instance.</item>
    /// </list>
    /// </remarks>
    /// <typeparam name="TServiceBuilder">The service builder type.</typeparam>
    /// <param name="builder">The Ocelot Builder instance, default.</param>
    /// <returns>The reference to the same extended <see cref="IOcelotBuilder"/> object.</returns>
    public static IOcelotBuilder AddConsul<TServiceBuilder>(this IOcelotBuilder builder)
        where TServiceBuilder : class, IConsulServiceBuilder
    {
        AddConsul(builder).Services
            .RemoveAll<IConsulServiceBuilder>()
            .AddScoped(typeof(IConsulServiceBuilder), typeof(TServiceBuilder));
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
