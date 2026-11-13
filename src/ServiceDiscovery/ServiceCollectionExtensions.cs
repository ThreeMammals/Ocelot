using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ocelot.ServiceDiscovery;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Forcibly adds the default <see cref="IDynamicRouting"/> implementation to the DI container, a.k.a. the default <see cref="UrlPathDynamicRouting"/> service.
    /// </summary>
    /// <param name="services">The services collection to add the feature to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> object.</returns>
    public static IServiceCollection AddOcelotDynamicRouting(this IServiceCollection services)
        => services.AddOcelotDynamicRouting<UrlPathDynamicRouting>();

    /// <summary>
    /// Forcibly adds a custom <see cref="IDynamicRouting"/> implementation to the DI container.
    /// </summary>
    /// <typeparam name="TDynamicRouting">The dynamic routing type.</typeparam>
    /// <param name="services">The services collection to add the feature to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> object.</returns>
    public static IServiceCollection AddOcelotDynamicRouting<TDynamicRouting>(this IServiceCollection services)
        where TDynamicRouting : class, IDynamicRouting
        => services.RemoveAll<IDynamicRouting>().AddSingleton<IDynamicRouting, TDynamicRouting>();
}
