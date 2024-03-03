using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace Ocelot.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds default ASP.NET services and Ocelot application services.<br/>
    /// Creates default <see cref="IConfiguration"/> from the current service descriptors.
    /// </summary>
    /// <remarks>
    /// Remarks for default ASP.NET services being injected see in docs of the <see cref="OcelotBuilder.AddDefaultAspNetServices(IMvcCoreBuilder, Assembly)"/> method.
    /// </remarks>
    /// <param name="services">Current services collection.</param>
    /// <returns>An <see cref="IOcelotBuilder"/> object.</returns>
    public static IOcelotBuilder AddOcelot(this IServiceCollection services)
    {
        var descriptor = services.Where(descriptor => descriptor.ServiceType == typeof(IConfiguration)).FirstOrDefault();
        var provider = new ServiceCollection().Add(descriptor).BuildServiceProvider();
        var configuration = provider.GetRequiredService<IConfiguration>();
        return new OcelotBuilder(services, configuration);
    }

    /// <summary>
    /// Adds default ASP.NET services and Ocelot application services with configuration.
    /// </summary>
    /// <remarks>
    /// Remarks for default ASP.NET services will be injected, see docs of the <see cref="OcelotBuilder.AddDefaultAspNetServices(IMvcCoreBuilder, Assembly)"/> method.
    /// </remarks>
    /// <param name="services">Current services collection.</param>
    /// <param name="configuration">Current web app configuration.</param>
    /// <returns>An <see cref="IOcelotBuilder"/> object.</returns>
    public static IOcelotBuilder AddOcelot(this IServiceCollection services, IConfiguration configuration)
    {
        return new OcelotBuilder(services, configuration);
    }

    /// <summary>
    /// Adds Ocelot application services and custom ASP.NET services with custom builder.<br/>
    /// Creates default <see cref="IConfiguration"/> from the current service descriptors.
    /// </summary>
    /// <remarks>
    /// Warning! To understand which ASP.NET services should be injected/removed by custom builder, see docs of the <see cref="OcelotBuilder.AddDefaultAspNetServices(IMvcCoreBuilder, Assembly)"/> method.
    /// </remarks>
    /// <param name="services">Current services collection.</param>
    /// <param name="customBuilder">Current custom builder for ASP.NET MVC pipeline.</param>
    /// <returns>An <see cref="IOcelotBuilder"/> object.</returns>
    public static IOcelotBuilder AddOcelotUsingBuilder(this IServiceCollection services, Func<IMvcCoreBuilder, Assembly, IMvcCoreBuilder> customBuilder)
    {
        var descriptor = services.Where(descriptor => descriptor.ServiceType == typeof(IConfiguration)).FirstOrDefault();
        var provider = new ServiceCollection().Add(descriptor).BuildServiceProvider();
        var configuration = provider.GetRequiredService<IConfiguration>();
        return new OcelotBuilder(services, configuration, customBuilder);
    }

    /// <summary>
    /// Adds Ocelot application services and custom ASP.NET services with configuration and custom builder.
    /// </summary>
    /// <remarks>
    /// Warning! To understand which ASP.NET services should be injected/removed by custom builder, see docs of the <see cref="OcelotBuilder.AddDefaultAspNetServices(IMvcCoreBuilder, Assembly)"/> method.
    /// </remarks>
    /// <param name="services">Current services collection.</param>
    /// <param name="configuration">Current web app configuration.</param>
    /// <param name="customBuilder">Current custom builder for ASP.NET MVC pipeline.</param>
    /// <returns>An <see cref="IOcelotBuilder"/> object.</returns>
    public static IOcelotBuilder AddOcelotUsingBuilder(this IServiceCollection services, IConfiguration configuration, Func<IMvcCoreBuilder, Assembly, IMvcCoreBuilder> customBuilder)
    {
        return new OcelotBuilder(services, configuration, customBuilder);
    }
}
