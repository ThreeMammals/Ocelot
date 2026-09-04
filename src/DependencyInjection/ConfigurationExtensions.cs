using Microsoft.Extensions.Configuration;
using Ocelot.Configuration.File;
using Ocelot.Infrastructure.Extensions;

namespace Ocelot.DependencyInjection;

/// <summary>
/// Provides convenience methods for retrieving Ocelot-specific configuration sections and properties from an <see cref="IConfiguration"/> instance.
/// </summary>
/// <remarks>
/// These helpers simplify access to the routes, dynamic routes, aggregates, and global configuration sections that Ocelot uses at runtime.
/// </remarks>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Gets the underlying <see cref="IConfigurationRoot"/> instance from the current configuration, if available.
    /// </summary>
    /// <param name="configuration">The configuration instance to inspect.</param>
    /// <returns>
    /// The root configuration when the current instance is an <see cref="IConfigurationRoot"/>; otherwise, <see langword="null"/>.
    /// </returns>
    public static IConfigurationRoot OcelotRoot(this IConfiguration configuration)
        => configuration as IConfigurationRoot; // in future we could attach any section, for example "Ocelot"

    /// <summary>
    /// Gets the routes section from the configuration.
    /// </summary>
    /// <param name="configuration">The configuration instance to inspect.</param>
    /// <returns>An <see cref="IConfigurationSection"/> representing the routes section.</returns>
    public static IConfigurationSection OcelotRoutes(this IConfiguration configuration)
        => configuration.GetSection(nameof(FileConfiguration.Routes));

    /// <summary>
    /// Gets the dynamic routes section from the configuration.
    /// </summary>
    /// <param name="configuration">The configuration instance to inspect.</param>
    /// <returns>An <see cref="IConfigurationSection"/> representing the dynamic routes section.</returns>
    public static IConfigurationSection OcelotDynamicRoutes(this IConfiguration configuration)
        => configuration.GetSection(nameof(FileConfiguration.DynamicRoutes));

    /// <summary>
    /// Gets the aggregates section from the configuration.
    /// </summary>
    /// <param name="configuration">The configuration instance to inspect.</param>
    /// <returns>An <see cref="IConfigurationSection"/> representing the aggregates section.</returns>
    public static IConfigurationSection OcelotAggregates(this IConfiguration configuration)
        => configuration.GetSection(nameof(FileConfiguration.Aggregates));

    /// <summary>
    /// Gets the global configuration section from the configuration.
    /// </summary>
    /// <param name="configuration">The configuration instance to inspect.</param>
    /// <returns>An <see cref="IConfigurationSection"/> representing the global configuration section.</returns>
    public static IConfigurationSection OcelotGlobalConfiguration(this IConfiguration configuration)
        => configuration.GetSection(nameof(FileConfiguration.GlobalConfiguration));

    /// <summary>
    /// Finds the first route section matching the specified upstream path template or key.
    /// </summary>
    /// <param name="configuration">The configuration instance to inspect.</param>
    /// <param name="upstreamPathTemplate">The upstream path template to match. If omitted, an empty value is used.</param>
    /// <param name="key">The route key to match. If omitted, an empty value is used.</param>
    /// <param name="comparison">The string comparison mode used to evaluate the route values.</param>
    /// <returns>
    /// The matching route section, or <see langword="null"/> if no route matches.
    /// </returns>
    public static IConfigurationSection OcelotRoute(this IConfiguration configuration,
        string upstreamPathTemplate = null, string key = null, StringComparison comparison = StringComparison.Ordinal)
    {
        key ??= string.Empty;
        upstreamPathTemplate ??= string.Empty;
        var routes = OcelotRoutes(configuration);
        return routes.GetChildren().FirstOrDefault(r =>
            upstreamPathTemplate.Equals(r[nameof(FileRoute.UpstreamPathTemplate)], comparison) ||
            key.Equals(r[nameof(FileRoute.Key)], comparison));
    }

    /// <summary>
    /// Finds the first dynamic route section matching the specified service name, namespace, or key.
    /// </summary>
    /// <param name="configuration">The configuration instance to inspect.</param>
    /// <param name="serviceName">The service name to match. If omitted, an empty value is used.</param>
    /// <param name="serviceNamespace">The service namespace to match. If omitted, an empty value is used.</param>
    /// <param name="key">The dynamic route key to match. If omitted, an empty value is used.</param>
    /// <param name="comparison">The string comparison mode used to evaluate the route values.</param>
    /// <returns>
    /// The matching dynamic route section, or <see langword="null"/> if no dynamic route matches.
    /// </returns>
    public static IConfigurationSection OcelotDynamicRoute(this IConfiguration configuration,
        string serviceName = null, string serviceNamespace = null, string key = null, StringComparison comparison = StringComparison.Ordinal)
    {
        key ??= string.Empty;
        serviceName ??= string.Empty;
        serviceNamespace ??= string.Empty;
        var routes = OcelotDynamicRoutes(configuration);
        return routes.GetChildren().FirstOrDefault(r =>
            (serviceNamespace.IsEmpty() && serviceName.Equals(r[nameof(FileDynamicRoute.ServiceName)], comparison)) ||
            (serviceNamespace.IsNotEmpty() && serviceName.Equals(r[nameof(FileDynamicRoute.ServiceName)], comparison) && serviceNamespace.Equals(r[nameof(FileDynamicRoute.ServiceNamespace)], comparison)) ||
            key.Equals(r[nameof(FileDynamicRoute.Key)], comparison));
    }

    /// <summary>
    /// Finds the first aggregate route section matching the specified upstream path template.
    /// </summary>
    /// <param name="configuration">The configuration instance to inspect.</param>
    /// <param name="upstreamPathTemplate">The upstream path template to match. If omitted, an empty value is used.</param>
    /// <param name="comparison">The string comparison mode used to evaluate the aggregate route values.</param>
    /// <returns>
    /// The matching aggregate route section, or <see langword="null"/> if no aggregate route matches.
    /// </returns>
    public static IConfigurationSection OcelotAggregate(this IConfiguration configuration, string upstreamPathTemplate, StringComparison comparison = StringComparison.Ordinal)
    {
        upstreamPathTemplate ??= string.Empty;
        var routes = OcelotAggregates(configuration);
        return routes.GetChildren().FirstOrDefault(r =>
            upstreamPathTemplate.Equals(r[nameof(FileAggregateRoute.UpstreamPathTemplate)], comparison));
    }

    /// <summary>
    /// Gets a named section from the current section or, if not found, from the global configuration section.
    /// </summary>
    /// <param name="configuration">The configuration instance to inspect.</param>
    /// <param name="sectionName">The section name to locate.</param>
    /// <param name="current">The section to search first. If <see langword="null"/>, the routes section is used.</param>
    /// <returns>
    /// The matching section, or <see langword="null"/> if no section is found.
    /// </returns>
    public static IConfigurationSection OcelotSection(this IConfiguration configuration, string sectionName, IConfigurationSection current = null)
    {
        current ??= OcelotRoutes(configuration);
        var property = current.GetChildren().FirstOrDefault(p => p.Key == sectionName);
        property ??= OcelotGlobalSection(configuration, sectionName);
        return property;
    }

    /// <summary>
    /// Gets a strongly typed value from a named section or from the global configuration section when no matching section is found.
    /// </summary>
    /// <typeparam name="T">The type of value to deserialize.</typeparam>
    /// <param name="configuration">The configuration instance to inspect.</param>
    /// <param name="propertyName">The property name to locate.</param>
    /// <param name="current">The section to search first. If <see langword="null"/>, the routes section is used.</param>
    /// <returns>
    /// The property value if found; otherwise, the <see langword="default"/> value for <typeparamref name="T"/>.
    /// </returns>
    public static T OcelotProperty<T>(this IConfiguration configuration, string propertyName, IConfigurationSection current = null)
    {
        current ??= OcelotRoutes(configuration);
        var property = OcelotSection(configuration, propertyName, current);
        return property is not null ? property.Get<T>() : default;
    }

    /// <summary>
    /// Gets a named section from the global configuration section.
    /// </summary>
    /// <param name="configuration">The configuration instance to inspect.</param>
    /// <param name="sectionName">The section name to locate.</param>
    /// <returns>
    /// The matching global section, or <see langword="null"/> if no section is found.
    /// </returns>
    public static IConfigurationSection OcelotGlobalSection(this IConfiguration configuration, string sectionName)
    {
        var global = OcelotGlobalConfiguration(configuration);
        return global.GetChildren().FirstOrDefault(p => p.Key == sectionName);
    }

    /// <summary>
    /// Gets a strongly typed value from the global configuration section.
    /// </summary>
    /// <typeparam name="T">The type of value to deserialize.</typeparam>
    /// <param name="configuration">The configuration instance to inspect.</param>
    /// <param name="propertyName">The property name to locate.</param>
    /// <returns>
    /// The property value if found; otherwise, the <see langword="default"/> value for <typeparamref name="T"/>.
    /// </returns>
    public static T OcelotGlobalProperty<T>(this IConfiguration configuration, string propertyName)
        => OcelotGlobalSection(configuration, propertyName).Get<T>();
}
