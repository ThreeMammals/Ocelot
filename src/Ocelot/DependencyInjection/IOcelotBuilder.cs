using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.LoadBalancer.Balancers;
using Ocelot.Multiplexer;
using Ocelot.ServiceDiscovery.Providers;

namespace Ocelot.DependencyInjection;

public interface IOcelotBuilder
{
    IServiceCollection Services { get; }

    IConfiguration Configuration { get; }

    IMvcCoreBuilder MvcCoreBuilder { get; }

    /// <summary>
    /// Adds a <see cref="DelegatingHandler"/> of the <paramref name="delegateType"/> type as a transient service, with the <paramref name="global"/> option to make the handler globally available.
    /// </summary>
    /// <param name="delegateType">The type of a <see cref="DelegatingHandler"/> to be registered.</param>
    /// <param name="global">True if the handler should be globally available.</param>
    /// <returns>The reference to the same <see cref="IOcelotBuilder"/> object.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Generates an exception if the <paramref name="delegateType"/> type does not inherit from the <see cref="DelegatingHandler"/>.</exception>
    IOcelotBuilder AddDelegatingHandler(Type delegateType, bool global = false);

    /// <summary>
    /// Adds a <see cref="DelegatingHandler"/> of the <typeparamref name="THandler"/> type as a transient service, with the <paramref name="global"/> option to make the handler globally available.
    /// </summary>
    /// <typeparam name="THandler">The type of a <see cref="DelegatingHandler"/> to be registered.</typeparam>
    /// <param name="global">True if the handler should be globally available.</param>
    /// <returns>The reference to the same <see cref="IOcelotBuilder"/> object.</returns>
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
}
