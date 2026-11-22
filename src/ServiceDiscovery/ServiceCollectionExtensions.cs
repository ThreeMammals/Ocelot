using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ocelot.ServiceDiscovery;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Forcibly adds the default <see cref="IDynamicRouting"/> implementation to the DI container, a.k.a. the default <see cref="UrlPathDynamicRouter"/> service.
    /// </summary>
    /// <param name="services">The services collection to add the feature to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> object.</returns>
    public static IServiceCollection AddOcelotDynamicRouting(this IServiceCollection services)
        => services.AddOcelotDynamicRouting<UrlPathDynamicRouter>();

    /// <summary>
    /// Forcibly adds a custom <see cref="IDynamicRouting"/> implementation to the DI container.
    /// </summary>
    /// <typeparam name="TDynamicRouting">The dynamic routing type.</typeparam>
    /// <param name="services">The services collection to add the feature to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> object.</returns>
    public static IServiceCollection AddOcelotDynamicRouting<TDynamicRouting>(this IServiceCollection services)
        where TDynamicRouting : class, IDynamicRouting
        => services.RemoveAll<IDynamicRouting>().AddSingleton<IDynamicRouting, TDynamicRouting>();

    /// <summary>
    /// Forcibly adds a custom <see cref="IDynamicRouting"/> implementation and configures multiple-segment routing options.
    /// </summary>
    /// <typeparam name="TDynamicRouting">The dynamic routing type.</typeparam>
    /// <param name="services">The services collection to add the feature to.</param>
    /// <param name="configureOptions">The action used to configure the options.</param>
    /// <returns>The same <see cref="IServiceCollection"/> object.</returns>
    public static IServiceCollection AddOcelotDynamicRouting<TDynamicRouting>(this IServiceCollection services, Action<MultipleSegmentUrlPathDynamicRoutingOptions> configureOptions)
        where TDynamicRouting : MultipleSegmentUrlPathDynamicRouting
        => services.AddOptions<MultipleSegmentUrlPathDynamicRoutingOptions>()
            .Configure(configureOptions)
            .Services.AddOcelotDynamicRouting<TDynamicRouting>();

    /// <summary>
    /// Registers a multiple-segment router and sets whether request headers can manage routing.
    /// </summary>
    /// <typeparam name="TDynamicRouting">The dynamic routing type.</typeparam>
    /// <param name="services">The services collection to add the feature to.</param>
    /// <param name="isHeaderManagement">Enable header management or disable.</param>
    /// <returns>The same <see cref="IServiceCollection"/> object.</returns>
    public static IServiceCollection AddOcelotDynamicRouting<TDynamicRouting>(this IServiceCollection services, bool isHeaderManagement)
        where TDynamicRouting : MultipleSegmentUrlPathDynamicRouting
        => services.AddOcelotDynamicRouting<TDynamicRouting>(options => options.IsHeaderManagement = isHeaderManagement);
}
