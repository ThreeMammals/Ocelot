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
    /// <typeparam name="TDynamicRouter">The dynamic router type.</typeparam>
    /// <param name="services">The services collection to add the feature to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> object.</returns>
    public static IServiceCollection AddOcelotDynamicRouting<TDynamicRouter>(this IServiceCollection services)
        where TDynamicRouter : class, IDynamicRouting
        => services.RemoveAll<IDynamicRouting>().AddSingleton<IDynamicRouting, TDynamicRouter>();

    /// <summary>
    /// Forcibly adds a custom <see cref="IDynamicRouting"/> implementation and configures multiple-segment routing options.
    /// </summary>
    /// <typeparam name="TDynamicRouter">The dynamic router type.</typeparam>
    /// <param name="services">The services collection to add the feature to.</param>
    /// <param name="configureOptions">The action used to configure the options.</param>
    /// <returns>The same <see cref="IServiceCollection"/> object.</returns>
    public static IServiceCollection AddOcelotDynamicRouting<TDynamicRouter>(this IServiceCollection services, Action<MultipleSegmentUrlPathDynamicRouterOptions> configureOptions)
        where TDynamicRouter : MultipleSegmentUrlPathDynamicRouter
        => services.AddOptions<MultipleSegmentUrlPathDynamicRouterOptions>()
            .Configure(configureOptions)
            .Services.AddOcelotDynamicRouting<TDynamicRouter>();

    /// <summary>
    /// Registers a multiple-segment router and sets whether request headers can manage routing.
    /// </summary>
    /// <typeparam name="TDynamicRouter">The dynamic router type.</typeparam>
    /// <param name="services">The services collection to add the feature to.</param>
    /// <param name="isHeaderManagement">Enable header management or disable.</param>
    /// <returns>The same <see cref="IServiceCollection"/> object.</returns>
    public static IServiceCollection AddOcelotDynamicRouting<TDynamicRouter>(this IServiceCollection services, bool isHeaderManagement)
        where TDynamicRouter : MultipleSegmentUrlPathDynamicRouter
        => services.AddOcelotDynamicRouting<TDynamicRouter>(options => options.IsHeaderManagement = isHeaderManagement);
}
