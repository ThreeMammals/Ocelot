using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Middleware;
using Ocelot.Multiplexer;
using Ocelot.QualityOfService;
using Ocelot.ServiceDiscovery.Providers;

namespace Ocelot.DependencyInjection;

public interface IOcelotBuilder
{
    IServiceCollection Services { get; }

    IConfiguration Configuration { get; }

    IMvcCoreBuilder MvcCoreBuilder { get; }

    /// <summary>
    /// Adds a delegate to create or process configuration on Ocelot startup when calling the <see cref="OcelotMiddlewareExtensions.UseOcelot(IApplicationBuilder)"/> methods.
    /// </summary>
    /// <remarks>Typically, configuration creation must be performed for configuration pollers (see <see cref="AddConfigurationPoller"/> helpers) or for other middleware services that depend on a real configuration object.</remarks>
    /// <param name="createConfiguration">The delegate to be added.</param>
    /// <returns>A reference to the same <see cref="IOcelotBuilder"/> object.</returns>
    IOcelotBuilder AddConfigurationDelegate(OcelotMiddlewareConfigurationDelegate createConfiguration);

    /// <summary>
    /// Adds the default functionality of the Configuration Poller feature using the default <see cref="FileConfigurationPoller"/> service.
    /// Reads the polling interval from the <see cref="InMemoryFileConfigurationPollerOptions"/>.
    /// The <see cref="DiskFileConfigurationRepository"/> service reads and saves the actual configuration from/to the disk.
    /// </summary>
    /// <returns>A reference to the same <see cref="IOcelotBuilder"/> object.</returns>
    IOcelotBuilder AddConfigurationPoller();

    /// <summary>
    /// Adds the Configuration Poller feature in service discovery mode to manage configuration being saved anywhere.
    /// Reads the polling interval from the <typeparamref name="TPollerOptions"/> service via the global <see cref="FileGlobalConfiguration.ServiceDiscoveryProvider"/> options.
    /// The <typeparamref name="TRepository"/> service reads and saves the actual configuration from/to any storage, including service discovery storage.
    /// </summary>
    /// <typeparam name="TPoller">The polling service.</typeparam>
    /// <typeparam name="TPollerOptions">The poller options.</typeparam>
    /// <typeparam name="TRepository">The repository service.</typeparam>
    /// <returns>A reference to the same <see cref="IOcelotBuilder"/> object.</returns>
    IOcelotBuilder AddConfigurationDiscoveryPoller<TPoller, TPollerOptions, TRepository>()
        where TPoller : class, IFileConfigurationPoller
        where TPollerOptions : ServiceDiscoveryFileConfigurationPollerOptions
        where TRepository : class, IFileConfigurationRepository;

    /// <summary>
    /// Adds the Configuration Poller feature to manage configuration that is saved anywhere.
    /// Reads the polling interval from the <typeparamref name="TPollerOptions"/> service.
    /// The <typeparamref name="TRepository"/> service reads and saves the actual configuration from or to any storage.
    /// </summary>
    /// <typeparam name="TPoller">The polling service type.</typeparam>
    /// <typeparam name="TPollerOptions">The poller options service type.</typeparam>
    /// <typeparam name="TRepository">The repository service type.</typeparam>
    /// <returns>A reference to the same <see cref="IOcelotBuilder"/> object.</returns>
    IOcelotBuilder AddConfigurationPoller<TPoller, TPollerOptions, TRepository>()
        where TPoller : class, IFileConfigurationPoller
        where TPollerOptions : class, IFileConfigurationPollerOptions
        where TRepository : class, IFileConfigurationRepository;

    /// <summary>
    /// Adds a <see cref="DelegatingHandler"/> of the <paramref name="delegateType"/> type as a transient service, with the <paramref name="global"/> option to make the handler globally available.
    /// </summary>
    /// <param name="delegateType">The type of a <see cref="DelegatingHandler"/> to be registered.</param>
    /// <param name="global">True if the handler should be globally available.</param>
    /// <returns>A reference to the same <see cref="IOcelotBuilder"/> object.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Generates an exception if the <paramref name="delegateType"/> type does not inherit from the <see cref="DelegatingHandler"/>.</exception>
    IOcelotBuilder AddDelegatingHandler(Type delegateType, bool global = false);

    /// <summary>
    /// Adds a <see cref="DelegatingHandler"/> of the <typeparamref name="THandler"/> type as a transient service, with the <paramref name="global"/> option to make the handler globally available.
    /// </summary>
    /// <typeparam name="THandler">The type of a <see cref="DelegatingHandler"/> to be registered.</typeparam>
    /// <param name="global">True if the handler should be globally available.</param>
    /// <returns>A reference to the same <see cref="IOcelotBuilder"/> object.</returns>
    IOcelotBuilder AddDelegatingHandler<THandler>(bool global = false)
        where THandler : DelegatingHandler;

    IOcelotBuilder AddSingletonDefinedAggregator<T>()
        where T : class, IDefinedAggregator;

    IOcelotBuilder AddTransientDefinedAggregator<T>()
        where T : class, IDefinedAggregator;

    IOcelotBuilder AddCustomLoadBalancer<T>()
        where T : ILoadBalancer, new();

    IOcelotBuilder AddCustomLoadBalancer<T>(Func<T> loadBalancerFactoryFunc)
        where T : ILoadBalancer;

    IOcelotBuilder AddCustomLoadBalancer<T>(Func<IServiceProvider, T> loadBalancerFactoryFunc)
        where T : ILoadBalancer;

    IOcelotBuilder AddCustomLoadBalancer<T>(
        Func<DownstreamRoute, IServiceDiscoveryProvider, T> loadBalancerFactoryFunc)
        where T : ILoadBalancer;

    IOcelotBuilder AddCustomLoadBalancer<T>(
        Func<IServiceProvider, DownstreamRoute, IServiceDiscoveryProvider, T> loadBalancerFactoryFunc)
        where T : ILoadBalancer;

    IOcelotBuilder AddConfigPlaceholders();

    /// <summary>
    /// Adds the built-in Quality of Service feature from the <c>Ocelot.QualityOfService</c> namespace via <see cref="QosDelegatingHandlerDelegate"/> after force-removing any other enabled external QoS implementations.
    /// </summary>
    /// <returns>A reference to the same <see cref="IOcelotBuilder"/> object.</returns>
    IOcelotBuilder AddQualityOfService();

    /// <summary>
    /// Adds a custom Quality of Service handler that inherits from <see cref="CircuitBreakerDelegatingHandler"/>,
    /// allowing overrides such as a custom <c>ServerErrorCodes</c> set.
    /// </summary>
    /// <typeparam name="THandler">A concrete subclass of <see cref="CircuitBreakerDelegatingHandler"/>.</typeparam>
    /// <returns>A reference to the same <see cref="IOcelotBuilder"/> object.</returns>
    IOcelotBuilder AddQualityOfService<THandler>()
        where THandler : CircuitBreakerDelegatingHandler;
}
